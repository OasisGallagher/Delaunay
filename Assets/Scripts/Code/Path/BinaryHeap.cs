using System.Collections.Generic;
namespace Delaunay
{
	public class BinaryHeap
	{
		const uint kNodeStateInHeap = 0x80000000;
		const uint kNodeStateClosed = 0x40000000;

		void SetNodeState(Triangle node, uint state)
		{
			Utility.Verify(!GetNodeState(node, state));
			node.Flag |= state;
		}

		void ClearNodeState(Triangle node, uint state)
		{
			Utility.Verify(GetNodeState(node, state));
			node.Flag &= (~state);
		}

		bool GetNodeState(Triangle node, uint state)
		{
			return (node.Flag & state) != 0;
		}

		void SetHeapIndex(Triangle node, int index)
		{
			node.Flag &= 0xC0000000;

			index &= 0x3FFFFFFF;
			node.Flag |= (uint)index;
		}

		int GetHeapIndex(Triangle node)
		{
			return (int)(node.Flag & 0x3FFFFFFF);
		}

		float F(Triangle node) { return node.G + node.H; }

		public void Push(Triangle node)
		{
			SetNodeState(node, kNodeStateInHeap);
			SetHeapIndex(node, container.Count);

			container.Add(node);

			AdjustHeap(node);
			Utility.Assert(IsHeap());
		}

		public void Dispose()
		{
			foreach (Triangle triangle in container)
			{
				triangle.ClearPathfinding();
			}

			container.Clear();

			foreach (Triangle triangle in close)
			{
				triangle.ClearPathfinding();
			}

			close.Clear();
		}

		public Triangle Pop()
		{
			Swap(0, container.Count - 1);
			Triangle result = container[container.Count - 1];

			close.Add(result);
			container.RemoveAt(container.Count - 1);

			ClearNodeState(result, kNodeStateInHeap);
			SetNodeState(result, kNodeStateClosed);

			int current = 0;
			for (; ; )
			{
				int min = current;
				int lchild = LeftChild(min), rchild = RightChild(min);
				if (lchild < container.Count && F(container[lchild]) < F(container[min]))
					min = lchild;

				if (rchild < container.Count && F(container[rchild]) < F(container[min]))
					min = rchild;

				if (min == current)
					break;

				Swap(min, current);

				current = min;
			}

			Utility.Assert(IsHeap());
			return result;
		}

		public void DecrGH(Triangle node, float newG, float newH)
		{
			Utility.Assert(newG + newH < node.G + node.H);

			node.G = newG;
			node.H = newH;

			AdjustHeap(node);

			Utility.Verify(IsHeap());
		}

		public bool Contains(Triangle node)
		{
			return GetNodeState(node, kNodeStateInHeap);
		}

		public bool IsClosed(Triangle node)
		{
			return GetNodeState(node, kNodeStateClosed);
		}

		public int Count
		{
			get { return container.Count; }
		}

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

		void AdjustHeap(Triangle node)
		{
			int parent = Parent(GetHeapIndex(node));
			for (; parent >= 0 && F(node) < F(container[parent]); parent = Parent(parent))
			{
				Swap(GetHeapIndex(node), parent);
			}
		}

		void Swap(int i, int j)
		{
			Triangle tmp = container[i];
			container[i] = container[j];
			container[j] = tmp;

			SetHeapIndex(container[i], i);
			SetHeapIndex(container[j], j);
		}

		int Parent(int i) { return (i - 1) / 2; }

		int LeftChild(int i) { return 2 * i + 1; }

		int RightChild(int i) { return 2 * i + 2; }

		List<Triangle> close = new List<Triangle>();
		List<Triangle> container = new List<Triangle>();
	}
}