using System;
using System.Collections.Generic;

namespace Delaunay
{
	/// <summary>
	/// 用于A*的最小堆容器. 
	/// <para>在A*结束后, 需要调用Dispose(), 清理容器中的节点.</para>
	/// </summary>
	public class AStarNodeContainer : IDisposable
	{
		/// <summary>
		/// 标记该节点不在堆中.
		/// </summary>
		const int kNodeStateOutOfHeap = -1;

		/// <summary>
		/// 标记该节点已被关闭.
		/// </summary>
		const int kNodeStateClosed = -2;

		/// <summary>
		/// 清理堆中残余的节点.
		/// </summary>
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

		/// <summary>
		/// 加入一个节点, 并调整堆结构.
		/// </summary>
		public void Push(PathfindingNode node)
		{
			node.Flag = container.Count;

			container.Add(node);

			AdjustHeap(node);
			Utility.Assert(IsHeap());
		}

		/// <summary>
		/// 关闭一个节点.
		/// </summary>
		public void Close(PathfindingNode node)
		{
			close.Add(node);
			node.Flag = kNodeStateClosed;
		}

		/// <summary>
		/// 弹出F值最小的节点, 并调整堆结构.
		/// </summary>
		/// <returns></returns>
		public PathfindingNode Pop()
		{
			Swap(0, container.Count - 1);
			PathfindingNode result = container[container.Count - 1];

			container.RemoveAt(container.Count - 1);

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

		/// <summary>
		/// 调整G和H. 
		/// <para>新的G+H必须比之前小.</para>
		/// </summary>
		public void DecreaseGH(PathfindingNode node, float newG, float newH)
		{
			Utility.Assert(newG + newH < node.G + node.H);

			node.G = newG;
			node.H = newH;

			AdjustHeap(node);

			Utility.Assert(IsHeap());
		}

		/// <summary>
		/// 节点是否在此次的A*中访问过.
		/// </summary>
		public bool IsVisited(PathfindingNode node)
		{
			return node.Flag >= 0;
		}

		/// <summary>
		/// 节点是否已经关闭.
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		public bool IsClosed(PathfindingNode node)
		{
			return node.Flag == kNodeStateClosed;
		}

		/// <summary>
		/// 当前堆中的元素个数.
		/// </summary>
		public int Count
		{
			get { return container.Count; }
		}

		/// <summary>
		/// 是否是合法的最小堆.
		/// </summary>
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

		/// <summary>
		/// 调整堆.
		/// </summary>
		void AdjustHeap(PathfindingNode node)
		{
			int parent = Parent(node.Flag);
			for (; parent >= 0 && F(node) < F(container[parent]); parent = Parent(parent))
			{
				Swap(node.Flag, parent);
			}
		}

		/// <summary>
		/// 交换container[i]和container[j].
		/// </summary>
		void Swap(int i, int j)
		{
			PathfindingNode tmp = container[i];
			container[i] = container[j];
			container[j] = tmp;

			container[i].Flag = i;
			container[j].Flag = j;
		}

		float F(PathfindingNode node) { return node.G + node.H; }

		/// <summary>
		/// 索引为i的节点的父节点.
		/// </summary>
		int Parent(int i) { return (i - 1) / 2; }

		/// <summary>
		/// 索引为i的节点的左孩子.
		/// </summary>
		int LeftChild(int i) { return 2 * i + 1; }

		/// <summary>
		/// 索引为i的节点的右孩子.
		/// </summary>
		int RightChild(int i) { return 2 * i + 2; }

		/// <summary>
		/// 已关闭的节点列表. 
		/// <para>存储它们的目的是在A*结束后, 清理使用过的节点的寻路相关数据.</para>
		/// </summary>
		List<PathfindingNode> close = new List<PathfindingNode>();

		/// <summary>
		/// 未关闭且在使用中的节点列表.
		/// </summary>
		List<PathfindingNode> container = new List<PathfindingNode>();
	}
}
