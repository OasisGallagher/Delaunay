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

					float newH = MathUtility.MinDistance(destPosition, portal.Src.Position, portal.Dest.Position);

					float newG = currentNode.G;
					if (currentNode.Portal != null)
					{
						newG += (currentNode.Portal.Center - portal.Center).magnitude2();
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

			//if (currentNode == destNode) { path = CreatePath(destNode); }

			container.Clear();

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

		static void TruncateByRadius(List<HalfEdge> path, float radius)
		{

		}
	}
}
