using System;
using System.Collections.Generic;
using UnityEngine;

namespace Delaunay
{
	/// <summary>
	/// 寻路使用的节点.
	/// </summary>
	public abstract class PathfindingNode
	{
		public PathfindingNode()
		{
			ClearPathfinding();
		}

		/// <summary>
		/// 获取该节点的包围边满足:
		/// <para>1. 该边和其Pair都是非约束的.</para>
		/// <para>2. 该边对面是可行走三角形.</para>
		/// </summary>
		public abstract HalfEdge[] AdjacencyPortals { get; }

		/// <summary>
		/// 起点到该节点的距离.
		/// </summary>
		public float G;

		/// <summary>
		/// 该节点到终点的估计距离.
		/// </summary>
		public float H;

		/// <summary>
		/// 堆标记.
		/// </summary>
		public int Flag;

		/// <summary>
		/// 进入该节点的入口边.
		/// </summary>
		public HalfEdge Portal;

		/// <summary>
		/// 重置寻路使用的数据.
		/// </summary>
		public void ClearPathfinding()
		{
			G = H = float.PositiveInfinity;
			Flag = -1;
			Portal = null;
		}
	}

	public static class Pathfinding
	{
		/// <summary>
		/// 计算半径为radius的物体, 从节点startNode, 位置startPosition到节点destNode, 位置destPosition的移动的路径.
		/// </summary>
		public static List<Vector3> FindPath(PathfindingNode startNode, Vector3 startPosition, PathfindingNode destNode, Vector3 destPosition, float radius)
		{
			List<HalfEdge> portals = AStarPathfinding.FindPath(startNode, startPosition, destNode, destPosition, radius);
			return PathSmoother.Smooth(startPosition, destPosition, portals, radius);
		}
	}

	public static class AStarPathfinding
	{
		/// <summary>
		/// 计算半径为radius的物体, 从节点startNode, 位置startPosition到节点destNode, 位置destPosition的移动, 经过的边.
		/// </summary>
		public static List<HalfEdge> FindPath(PathfindingNode startNode, Vector3 startPosition, PathfindingNode destNode, Vector3 destPosition, float radius)
		{
			AStarNodeContainer container = new AStarNodeContainer();

			startNode.G = 0f;
			startNode.H = 0f;

			container.Push(startNode);

			PathfindingNode currentNode = null;

			for (; container.Count != 0 && currentNode != destNode; )
			{
				currentNode = container.Pop();

				foreach (HalfEdge portal in currentNode.AdjacencyPortals)
				{
					// 忽略不可行走的三角形已经被关闭的三角形.
					if (!portal.Face.Walkable || container.IsClosed(portal.Face))
					{
						continue;
					}

					// 可否进入节点.
					if (!CheckEntryAndExitWidthLimit(currentNode, destNode, portal, radius))
					{
						continue;
					}

					// 可否进入节点, 如果非终点的话, 可否有边供离开.
					if (!CheckCorridorWidthLimit(portal.Pair.Face.Portal, portal, radius))
					{
						continue;
					}

					if (!container.IsVisited(portal.Face))
					{
						container.Push(portal.Face);
					}

					Utility.Verify(currentNode.G == 0 || currentNode.Portal != null);

					// https://raygun.com/blog/2015/01/game-development-triangulated-spaces-part-2/
					float newH = MathUtility.MinDistance2Segment(destPosition, portal.Src.Position, portal.Dest.Position);
					float newG = MathUtility.MinDistance2Segment(startPosition, portal.Src.Position, portal.Dest.Position);
					
					if (currentNode.Portal != null)
					{
						newG = Mathf.Max(newG, (currentNode.H - newH) + currentNode.G);
						newG = Mathf.Max(newG, CalculateArcLengthBetweenPortals(currentNode.Portal, portal, radius));
					}

					// 更新G和H, 以及入口.
					if (newG + newH < portal.Face.G + portal.Face.H)
					{
						container.DecreaseGH(portal.Face, newG, newH);
						portal.Face.Portal = portal;
					}
				}

				container.Close(currentNode);
			}

			List<HalfEdge> path = CreatePath(currentNode);

			// Create truncated path if currentNode != destNode.
			//if (currentNode == destNode) { path = CreatePath(destNode); }

			container.Dispose();

			return path;
		}

		/// <summary>
		/// 判断半径为radius的物体, 可否通过portal进入currentNode.
		/// </summary>
		static bool CheckEntryAndExitWidthLimit(PathfindingNode currentNode, PathfindingNode destNode, HalfEdge portal, float radius)
		{
			// 如果已经到达最终节点, 只判断可否经由此边进入.
			if (currentNode == destNode)
			{
				return (portal.Dest.Position - portal.Src.Position).magnitude2() >= radius * 2;
			}

			// 否则, 除了判断, 是否可以经由次边进入外, 仍需判断该节点是否存在其他的边, 供离开.
			HalfEdge other1 = portal.Face.AB, other2 = portal.Face.BC;
			if (portal == portal.Face.AB)
			{
				other1 = portal.Face.BC;
				other2 = portal.Face.CA;
			}
			else if (portal == portal.Face.BC)
			{
				other1 = portal.Face.AB;
				other2 = portal.Face.CA;
			}

			// 其他两条边是否可供离开.
			float diameter = radius * 2f;
			if (portal.Face.GetWidth(portal, other1) >= diameter
				|| portal.Face.GetWidth(portal, other2) >= diameter)
			{
				return true;
			}

			return false;
		}

		/// <summary>
		/// 判断半径为radius的物体是否可以通过prevPortal和currentPortal.
		/// </summary>
		static bool CheckCorridorWidthLimit(HalfEdge prevPortal, HalfEdge currentPortal, float radius)
		{
			if (prevPortal == null)
			{
				return true;
			}

			return currentPortal.Pair.Face.GetWidth(prevPortal, currentPortal.Pair) >= (radius * 2f);
		}

		/// <summary>
		/// 创建到dest的经过的边的集合.
		/// </summary>
		static List<HalfEdge> CreatePath(PathfindingNode dest)
		{
			List<HalfEdge> result = new List<HalfEdge>();
			for (HalfEdge entry; (entry = dest.Portal) != null; dest = entry.Pair.Face)
			{
				Utility.Verify(result.Count < 1024, "Too many waypoints");
				result.Add(entry);
			}

			// 创建的集合为从dest到start, 所以将它反序.
			result.Reverse();

			return result;
		}

		/// <summary>
		/// 获取圆心在portal1和portal2的交点, 角度为二者的夹角, 半径为radius的圆弧的长度.
		/// </summary>
		public static float CalculateArcLengthBetweenPortals(HalfEdge portal1, HalfEdge portal2, float radius)
		{
			Vector3 v1 = Vector3.zero, v2 = Vector3.zero;

			if (portal1.Src == portal2.Src)
			{
				v1 = portal1.Dest.Position - portal1.Src.Position; 
				v2 = portal2.Dest.Position - portal1.Src.Position;
			}
			else if (portal1.Dest == portal2.Dest)
			{
				v1 = portal1.Src.Position - portal1.Dest.Position;
				v2 = portal2.Src.Position - portal1.Dest.Position;
			}
			else
			{
				Utility.Verify(false, "failed to find common vertex, portals are {0} and {1}", portal1, portal2);
			}

			return Mathf.Acos(v1.dot2(v2) / (v1.magnitude2() * v2.magnitude2())) * radius;
		}
	}
}
