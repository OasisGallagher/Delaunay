using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
namespace Delaunay
{
	public static class AStarPathfinding
	{
		public static List<HalfEdge> FindPath(Triangle start, Triangle dest)
		{
			BinaryHeap open = new BinaryHeap();
			HashSet<Triangle> close = new HashSet<Triangle>();
			
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

			return start == dest ? CreatePath(dest) : null;
		}

		static List<HalfEdge> CreatePath(Triangle dest)
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

		internal class BinaryHeap
		{
			public void Push(Triangle node)
			{
				container.Add(node);

				AdjustAt(container.Count - 1);
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
					if (lchild < container.Count && container[lchild].F < container[min].F)
					{
						min = lchild;
					}

					if (rchild < container.Count && container[rchild].F < container[min].F)
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
			public int IndexOf(Triangle node)
			{
				return container.IndexOf(node);
			}

			bool IsHeap()
			{
				for (int i = 1; i < container.Count; ++i)
				{
					if (container[Parent(i)].F > container[i].F)
					{
						return false;
					}
				}

				return true;
			}

			void AdjustAt(int index)
			{
				int parent = Parent(index);
				for (; parent >= 0 && container[index].F < container[parent].F; )
				{
					Swap(index, parent);
					index = parent;
					parent = Parent(parent);
				}
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
