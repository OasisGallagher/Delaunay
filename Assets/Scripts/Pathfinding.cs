using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
namespace Delaunay
{
	public static class AStarPathfinding
	{
		public static List<Triangle> FindPath(Triangle start, Triangle dest)
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

					if (!open.Contains(current.Face))
					{
						current.Face.G = float.PositiveInfinity;
						open.Push(current.Face);
					}

					float tmp = start.G + start.CalcWeight(current);
					if (tmp < current.Face.G)
					{
						open.DecrG(current.Face, tmp);
						current.Face.Parent = start;
					}
				}

				close.Add(start);
			}

			return open.Count == 0 ? null : CreatePath(dest);
		}

		static List<Triangle> CreatePath(Triangle dest)
		{
			List<Triangle> result = new List<Triangle>();
			for (; dest != null; dest = dest.Parent)
			{
				result.Add(dest);
			}

			result.Reverse();

			return result;
		}

		internal class BinaryHeap
		{
			public void Push(Triangle node)
			{
				node.HeapIndex = container.Count;
				container.Add(node);

				AdjustAt(node);

				Utility.Verify(IsHeap());
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
						min = lchild;

					if (rchild < container.Count && container[rchild].F < container[min].F)
						min = rchild;

					if (min == current)
						break;

					Swap(min, current);

					current = min;
				}

				result.HeapIndex = -1;

				Utility.Verify(IsHeap());
				return result;
			}

			public void DecrG(Triangle node, float value)
			{
				Utility.Verify(node.G > value);

				node.G = value;

				AdjustAt(node);

				Utility.Verify(IsHeap());
			}

			public bool Contains(Triangle node)
			{
				return node.HeapIndex != -1;
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

			void AdjustAt(Triangle node)
			{
				int parent = Parent(node.HeapIndex);
				for (; parent >= 0 && node.F < container[parent].F; parent = Parent(parent))
				{
					Swap(node.HeapIndex, parent);
				}
			}

			void Swap(int i, int j)
			{
				Triangle tmp = container[i];
				container[i] = container[j];
				container[j] = tmp;

				container[i].HeapIndex = i;
				container[j].HeapIndex = j;
			}

			int Parent(int i) { return (i - 1) / 2; }

			int LeftChild(int i) { return 2 * i + 1; }

			int RightChild(int i) { return 2 * i + 2; }

			List<Triangle> container = new List<Triangle>();
		}
	}
}
