using System;
using System.Collections.Generic;
using UnityEngine;

namespace Delaunay
{
	public abstract class PathfindingNode
	{
		public PathfindingNode()
		{
			ClearPathfinding();
		}

		public abstract HalfEdge[] AdjPortals { get; }

		public float G;
		public float H;
		public int Flag;
		public HalfEdge Portal;

		public void ClearPathfinding()
		{
			G = H = float.PositiveInfinity;
			Flag = -1;
			Portal = null;
		}
	}

	public static class Pathfinding
	{
		public static List<Vector3> FindPath(Vector3 startPosition, Vector3 destPosition, PathfindingNode startNode, PathfindingNode destNode, float radius)
		{
			List<HalfEdge> portals = AStarPathfinding.FindPath(startPosition, destPosition, startNode, destNode, radius);
			return PathSmooth.Smooth(startPosition, destPosition, portals, radius);
		}
	}

	public static class AStarPathfinding
	{
		public static List<HalfEdge> FindPath(Vector3 startPosition, Vector3 destPosition, 
			PathfindingNode startNode, PathfindingNode destNode, 
			float radius)
		{
			BinaryHeap heap = new BinaryHeap();

			startNode.G = 0;
			heap.Push(startNode);

			PathfindingNode currentNode = null;

			for (; heap.Count != 0 && currentNode != destNode; )
			{
				currentNode = heap.Pop();

				foreach (HalfEdge current in currentNode.AdjPortals)
				{
					if (!current.Face.Walkable || heap.IsClosed(current.Face))
					{
						continue;
					}

					if (!CheckEntranceWidthLimit(currentNode, startNode, destNode, current, radius))
					{
						continue;
					}

					if (!heap.Contains(current.Face))
					{
						heap.Push(current.Face);
					}

					Utility.Verify(currentNode.G == 0 || currentNode.Portal != null);

					if (!CheckCorridorWidthLimit(currentNode, startNode, destNode, current, radius))
					{
						continue;
					}

					float newH = MathUtility.MinDistance(destPosition, current.Src.Position, current.Dest.Position);

					float newG = currentNode.G;
					if (currentNode.Portal != null)
					{
						newG += (currentNode.Portal.Center - current.Center).magnitude2();
					}

					if (newG + newH < current.Face.G + current.Face.H)
					{
						heap.DecrGH(current.Face, newG, newH);
						current.Face.Portal = current;
					}
				}
			}

			List<HalfEdge> path = CreatePath(currentNode);
			//if (currentNode == destNode) { path = CreatePath(destNode); }

			heap.Dispose();

			return path;
		}

		static bool CheckEntranceWidthLimit(PathfindingNode currentNode, PathfindingNode startNode, PathfindingNode destNode, HalfEdge current, float radius)
		{
			if (currentNode == destNode)
			{
				// TODO: 
				return true;
			}

			HalfEdge other1 = current.Face.AB, other2 = current.Face.BC;
			if (current == current.Face.AB)
			{
				other1 = current.Face.BC;
				other2 = current.Face.CA;
			}
			else if (current == current.Face.BC)
			{
				other1 = current.Face.AB;
				other2 = current.Face.CA;
			}

			float diameter = radius * 2f;
			if (current.Face.GetWidth(current, other1) >= diameter
				|| current.Face.GetWidth(current, other2) >= diameter)
			{
				return true;
			}

			return false;
		}

		static bool CheckCorridorWidthLimit(PathfindingNode currentNode, PathfindingNode startNode, PathfindingNode destNode, HalfEdge current, float radius)
		{
			if (currentNode == startNode)
			{
				// ???
				return true;
			}

			HalfEdge lastPortal = current.Pair.Face.Portal;

			if (lastPortal == null)
			{
				return true;
			}

			float diameter = radius * 2f;
			return current.Pair.Face.GetWidth(lastPortal, current.Pair) >= diameter;
		}

		static List<HalfEdge> CreatePath(PathfindingNode dest)
		{
			List<HalfEdge> result = new List<HalfEdge>();
			for (HalfEdge entry; (entry = dest.Portal) != null; dest = entry.Pair.Face)
			{
				Utility.Verify(result.Count < 1024, "Too many waypoints");
				result.Add(entry);
			}

			result.Reverse();

			return result;
		}
	}
}
