using System.Collections.Generic;
namespace Delaunay
{
	public class BinaryHeap
	{
		const int kNodeStateOutOfHeap = -1;
		const int kNodeStateClosed = -2;

		float F(PathfindingNode node) { return node.G + node.H; }

		public void Push(PathfindingNode node)
		{
			node.Flag = container.Count;

			container.Add(node);

			AdjustHeap(node);
			Utility.Assert(IsHeap());
		}

		public void Dispose()
		{
			foreach (PathfindingNode node in container)
			{
				node.ClearPathfinding();
			}

			container.Clear();

			foreach (PathfindingNode node in close)
			{
				node.ClearPathfinding();
			}

			close.Clear();
		}

		public PathfindingNode Pop()
		{
			Swap(0, container.Count - 1);
			PathfindingNode result = container[container.Count - 1];

			close.Add(result);
			container.RemoveAt(container.Count - 1);

			result.Flag = kNodeStateClosed;

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

		public void DecrGH(PathfindingNode node, float newG, float newH)
		{
			Utility.Assert(newG + newH < node.G + node.H);

			node.G = newG;
			node.H = newH;

			AdjustHeap(node);

			Utility.Verify(IsHeap());
		}

		public bool Contains(PathfindingNode node)
		{
			return node.Flag >= 0;
		}

		public bool IsClosed(PathfindingNode node)
		{
			return node.Flag == kNodeStateClosed;
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

		void AdjustHeap(PathfindingNode node)
		{
			int parent = Parent(node.Flag);
			for (; parent >= 0 && F(node) < F(container[parent]); parent = Parent(parent))
			{
				Swap(node.Flag, parent);
			}
		}

		void Swap(int i, int j)
		{
			PathfindingNode tmp = container[i];
			container[i] = container[j];
			container[j] = tmp;

			container[i].Flag = i;
			container[j].Flag = j;
		}

		int Parent(int i) { return (i - 1) / 2; }

		int LeftChild(int i) { return 2 * i + 1; }

		int RightChild(int i) { return 2 * i + 2; }

		List<PathfindingNode> close = new List<PathfindingNode>();
		List<PathfindingNode> container = new List<PathfindingNode>();
	}
}