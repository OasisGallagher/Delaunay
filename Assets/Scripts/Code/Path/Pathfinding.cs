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

		public abstract HalfEdge[] AdjacencyPortals { get; }

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
		public static List<Vector3> FindPath(PathfindingNode startNode, Vector3 startPosition, PathfindingNode destNode, Vector3 destPosition, float radius)
		{
			List<HalfEdge> portals = AStarPathfinding.FindPath(startNode, startPosition, destNode, destPosition, radius);
			return PathSmoother.Smooth(startPosition, destPosition, portals, radius);
		}
	}

	public static class AStarPathfinding
	{
		public static List<HalfEdge> FindPath(PathfindingNode startNode, Vector3 startPosition, PathfindingNode destNode, Vector3 destPosition, float radius)
		{
			AStarNodeContainer container = new AStarNodeContainer();

			startNode.G = 0;
			container.Push(startNode);

			PathfindingNode currentNode = null;

			for (; container.Count != 0 && currentNode != destNode; )
			{
				currentNode = container.Pop();

				foreach (HalfEdge portal in currentNode.AdjacencyPortals)
				{
					if (!portal.Face.Walkable || container.IsClosed(portal.Face))
					{
						continue;
					}

					if (!CheckEntryAndExitWidthLimit(currentNode, destNode, portal, radius))
					{
						continue;
					}

					if (!CheckCorridorWidthLimit(portal.Pair.Face.Portal, portal, radius))
					{
						continue;
					}

					if (!container.Contains(portal.Face))
					{
						container.Push(portal.Face);
					}

					Utility.Verify(currentNode.G == 0 || currentNode.Portal != null);

					// https://raygun.com/blog/2015/01/game-development-triangulated-spaces-part-2/

					float newH = MathUtility.MinDistance(destPosition, portal.Src.Position, portal.Dest.Position);

					float newG = MathUtility.MinDistance(startPosition, portal.Src.Position, portal.Dest.Position);
					newG = Mathf.Max(newG, (currentNode.H - newH) + currentNode.G);

					if (currentNode.Portal != null)
					{
						newG = Mathf.Max(newG, CalculateArcLengthBetweenPortals(currentNode.Portal, portal, radius));
					}

					if (newG + newH < portal.Face.G + portal.Face.H)
					{
						container.DecreaseGH(portal.Face, newG, newH);
						portal.Face.Portal = portal;
					}
				}

				container.Close(currentNode);
			}

			List<HalfEdge> path = CreatePath(currentNode);
			TruncateByRadius(path, radius);

			// Create truncated path if currentNode != destNode.
			//if (currentNode == destNode) { path = CreatePath(destNode); }

			container.Dispose();

			return path;
		}

		static bool CheckEntryAndExitWidthLimit(PathfindingNode currentNode, PathfindingNode destNode, HalfEdge portal, float radius)
		{
			if (currentNode == destNode)
			{
				return (portal.Dest.Position - portal.Src.Position).magnitude2() >= radius * 2;
			}

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

			float diameter = radius * 2f;
			if (portal.Face.GetWidth(portal, other1) >= diameter
				|| portal.Face.GetWidth(portal, other2) >= diameter)
			{
				return true;
			}

			return false;
		}

		static bool CheckCorridorWidthLimit(HalfEdge prevPortal, HalfEdge currentPortal, float radius)
		{
			if (prevPortal == null)
			{
				return true;
			}

			return currentPortal.Pair.Face.GetWidth(prevPortal, currentPortal.Pair) >= (radius * 2f);
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

		public static float CalculateArcLengthBetweenPortals(HalfEdge portal1, HalfEdge portal2, float radius)
		{
			Vector3 v1 = Vector3.zero, v2 = Vector3.zero;

			// TODO: Any good idea?
			if (portal1.Src == portal2.Src)
			{
				v1 = portal1.Dest.Position - portal1.Src.Position; v2 = portal2.Dest.Position - portal1.Src.Position;
			}
			else if (portal1.Src == portal2.Dest)
			{
				v1 = portal1.Dest.Position - portal1.Src.Position; v2 = portal2.Src.Position - portal1.Src.Position;
			}
			else if (portal1.Dest == portal2.Src)
			{
				v1 = portal1.Src.Position - portal1.Dest.Position; v2 = portal2.Dest.Position - portal1.Dest.Position;
			}
			else if (portal1.Dest == portal2.Dest)
			{
				v1 = portal1.Src.Position - portal1.Dest.Position; v2 = portal2.Src.Position - portal1.Dest.Position;
			}
			else
			{
				Utility.Verify(false, "failed to find common vertex, portals are {0} and {1}", portal1, portal2);
			}

			return Mathf.Acos(v1.dot2(v2) / (v1.magnitude2() * v2.magnitude2())) * radius;
		}

		static void TruncateByRadius(List<HalfEdge> path, float radius)
		{

		}
	}
}
