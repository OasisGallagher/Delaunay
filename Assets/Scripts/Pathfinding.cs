using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
namespace Delaunay
{
	public interface IPathNode
	{
		HalfEdge Entry { get; set; }
		List<HalfEdge> AdjNodes { get; }

		float G { get; set; }
		float H { get; set; }

		float CalcWeight(HalfEdge other);
	}

	public static class AStarPathfinding
	{
		public static List<HalfEdge> FindPath(IPathNode start, IPathNode dest)
		{
			BinaryHeap open = new BinaryHeap();
			HashSet<IPathNode> close = new HashSet<IPathNode>();
			
			start.G = 0;

			open.Push(start);

			for (; open.Count != 0 && start != dest; )
			{
				start = open.Pop();

				foreach (HalfEdge current in start.AdjNodes)
				{
					if (!current.Face.Walkable || close.Contains(current.Face))
					{
						continue;
					}

					int index = open.IndexOf(current.Face);
					if (index < 0)
					{
						current.Face.Entry = null;
						current.Face.G = float.PositiveInfinity;
						open.Push(current.Face);
						index = open.Count - 1;
					}

					float tmp = start.G + start.CalcWeight(current);
					if (tmp < current.Face.G)
					{
						open.DecrG(index, tmp);
						current.Face.Entry = current;
					}
				}

				close.Add(start);
			}

			open.Dispose();

			return start == dest ? CreatePath(dest) : null;
		}

		static List<HalfEdge> CreatePath(IPathNode dest)
		{
			List<HalfEdge> result = new List<HalfEdge>();
			for(HalfEdge entry; (entry = dest.Entry) != null; dest = entry.Pair.Face)
			{
				Utility.Verify(result.Count < 1024, "Too many waypoints");
				result.Add(entry);
			}

			result.Reverse();

			return result;
		}

		internal class BinaryHeap : IDisposable
		{
			public void Dispose() { container.ForEach(item => { item.Entry = null; }); }
			public void Push(IPathNode node)
			{
				container.Add(node);

				AdjustAt(container.Count - 1);
			}

			public int Count { get { return container.Count; } }

			public IPathNode Pop()
			{
				Swap(0, container.Count - 1);
				IPathNode result = container[container.Count - 1];
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

			public void DecrG(int index, float value)
			{
				Utility.Verify(container[index].G > value);

				container[index].G = value;

				AdjustAt(index);
			}

			// TODO: O(n).
			public int IndexOf(IPathNode node)
			{
				return container.IndexOf(node);
			}

			float F(IPathNode node) { return node.G + node.H; }

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

			void AdjustAt(int index)
			{
				int parent = Parent(index);
				for (; parent >= 0 && F(container[index]) < F(container[parent]); )
				{
					Swap(index, parent);
					index = parent;
					parent = Parent(parent);
				}
			}

			void Swap(int i, int j)
			{
				IPathNode tmp = container[i];
				container[i] = container[j];
				container[j] = tmp;
			}

			int Parent(int i) { return (i - 1) / 2; }

			int LeftChild(int i) { return 2 * i + 1; }

			int RightChild(int i) { return 2 * i + 2; }

			List<IPathNode> container = new List<IPathNode>();
		}
	}

	public static class PathSmoother
	{
		public static List<Vector3> Smooth(Vector3 start, Vector3 dest, List<HalfEdge> edges)
		{
			Vector3[] portals = new Vector3[edges.Count * 2 + 4];
			portals[0] = portals[1] = start;
			int index = 2;

			foreach(HalfEdge edge in edges)
			{
				portals[index++] = edge.Src.Position;
				portals[index++] = edge.Dest.Position;
			}

			portals[index] = portals[index + 1] = dest;
			edges.ForEach(e => { e.Face.Entry = null; });

			return StringPull(portals);
		}

		static List<Vector3> StringPull(Vector3[] portals)
		{
			Vector3 portalApex = portals[0];
			Vector3 portalLeft = portals[2];
			Vector3 portalRight = portals[3];

			List<Vector3> answer = new List<Vector3>(1 + portals.Length / 2);
			answer.Add(portalApex);

			int apexIndex = 0, leftIndex = 0, rightIndex = 0;
			for (int i = 1; i < portals.Length / 2; ++i)
			{
				Vector3 left = portals[i * 2];
				Vector3 right = portals[i * 2 + 1];

				if (Utility.Cross2D(right, portalRight, portalApex) <= 0)
				{
					if (Utility.Equals2D(portalApex, portalRight) || Utility.Cross2D(right, portalLeft, portalApex) > 0f)
					{
						portalRight = right;
						rightIndex = i;
					}
					else
					{
						answer.Add(portalLeft);
						portalApex = portalLeft;
						apexIndex = leftIndex;

						portalLeft = portalRight = portalApex;
						leftIndex = rightIndex = apexIndex;

						i = apexIndex;
						continue;
					}
				}

				if (Utility.Cross2D(left, portalLeft, portalApex) >= 0)
				{
					if (Utility.Equals2D(portalApex, portalLeft) || Utility.Cross2D(left, portalRight, portalApex) < 0f)
					{
						portalLeft = left;
						leftIndex = i;
					}
					else
					{
						answer.Add(portalRight);
						portalApex = portalRight;
						apexIndex = rightIndex;

						portalLeft = portalRight = portalApex;
						leftIndex = rightIndex = apexIndex;

						i = apexIndex;
						continue;
					}
				}
			}

			answer.Add(portals[portals.Length  -1]);

			return answer;
		}
	}
}
