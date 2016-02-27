using System;
using System.Collections.Generic;
using UnityEngine;
namespace Delaunay
{
	public static class Pathfinding
	{
		public static List<Vector3> FindPath(Vector3 startPosition, Vector3 destPosition, Triangle startNode, Triangle destNode, float radius)
		{
			List<HalfEdge> portals = AStarPathfinding.FindPath(startPosition, destPosition, startNode, destNode, radius);
			return PathSmoother.Smooth(startPosition, destPosition, portals, 0.5f);
		}
	}

	internal static class AStarPathfinding
	{
		public static List<HalfEdge> FindPath(Vector3 startPosition, Vector3 destPosition, Triangle startNode, Triangle destNode, float radius)
		{
			BinaryHeap open = new BinaryHeap();
			CloseList close = new CloseList();

			startNode.G = 0;
			open.Push(startNode);

			Triangle currentNode = null;

			for (; open.Count != 0 && currentNode != destNode; )
			{
				currentNode = open.Pop();

				foreach (HalfEdge current in currentNode.AdjPortals)
				{
					if (!current.Face.Walkable || close.Contains(current.Face))
					{
						continue;
					}

					if (!CheckEntranceWidthLimit(currentNode, startNode, destNode, current, radius))
					{
						continue;
					}

					int index = open.IndexOf(current.Face);
					if (index < 0)
					{
						current.Face.Portal = null;
						current.Face.G = float.PositiveInfinity;
						current.Face.H = float.PositiveInfinity;

						open.Push(current.Face);
						index = open.Count - 1;
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
						current.Face.H = newH;
						current.Face.G = newG;
						open.AdjustHeap(index);
						current.Face.Portal = current;
					}
				}

				close.Add(currentNode);
			}

			List<HalfEdge> path = null;
			if (currentNode == destNode) { path = CreatePath(destNode); }

			open.Dispose();
			close.Dispose();

			return path;
		}

		static bool CheckEntranceWidthLimit(Triangle currentNode, Triangle startNode, Triangle destNode, HalfEdge current, float radius)
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

			if (current.Face.GetWidth(current, other1) >= radius
				|| current.Face.GetWidth(current, other2) >= radius)
			{
				return true;
			}

			return false;
		}

		static bool CheckCorridorWidthLimit(Triangle currentNode, Triangle startNode, Triangle destNode, HalfEdge current, float radius)
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

			return current.Pair.Face.GetWidth(lastPortal, current.Pair) >= radius;
		}

		static List<HalfEdge> CreatePath(Triangle dest)
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

		internal class CloseList : IDisposable
		{
			public void Dispose()
			{
				foreach (Triangle node in container) { node.Portal = null; }
			}

			public void Add(Triangle item)
			{
				container.Add(item);
			}

			public bool Contains(Triangle item)
			{
				return container.Contains(item);
			}

			HashSet<Triangle> container = new HashSet<Triangle>();
		}

		internal class BinaryHeap : IDisposable
		{
			public void Dispose()
			{
				container.ForEach(item => { item.Portal = null; });
			}

			public void Push(Triangle node)
			{
				container.Add(node);

				AdjustHeap(container.Count - 1);
			}

			public int Count { get { return container.Count; } }

			public Triangle Pop()
			{
				Swap(0, container.Count - 1);
				Triangle result = container[container.Count - 1];
				container.RemoveAt(container.Count - 1);

				int current = 0;
				for (; ; )
				{
					int min = current;
					int lchild = LeftChild(min), rchild = RightChild(min);
					if (lchild < container.Count && F(container[lchild]) < F(container[min]))
					{
						min = lchild;
					}

					if (rchild < container.Count && F(container[rchild]) < F(container[min]))
					{
						min = rchild;
					}

					if (min == current)
					{
						break;
					}

					Swap(min, current);

					current = min;
				}

				return result;
			}

			public void AdjustHeap(int from)
			{
				int parent = Parent(from);
				for (; parent >= 0 && F(container[from]) < F(container[parent]); )
				{
					Swap(from, parent);
					from = parent;
					parent = Parent(parent);
				}
			}

			// TODO: O(n).
			public int IndexOf(Triangle node)
			{
				return container.IndexOf(node);
			}

			float F(Triangle node) { return node.G + node.H; }

			bool IsHeap()
			{
				for (int i = 1; i < container.Count; ++i)
				{
					if (F(container[Parent(i)]) > F(container[i]))
					{
						return false;
					}
				}

				return true;
			}

			void Swap(int i, int j)
			{
				Triangle tmp = container[i];
				container[i] = container[j];
				container[j] = tmp;
			}

			int Parent(int i) { return (i - 1) / 2; }

			int LeftChild(int i) { return 2 * i + 1; }

			int RightChild(int i) { return 2 * i + 2; }

			List<Triangle> container = new List<Triangle>();
		}
	}
}
