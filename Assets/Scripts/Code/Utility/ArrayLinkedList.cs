using System;
using System.Collections.Generic;

namespace Delaunay
{
	/// <summary>
	/// 用数组模拟的链表.
	/// </summary>
	public class ArrayLinkedList<T>
	{
		public struct Enumerator
		{
			public Enumerator(ArrayLinkedList<T> list)
			{
				this.list = list;
				this.currentIndex = int.MinValue;
			}

			/// <summary>
			/// 当前的数组索引.
			/// </summary>
			public int CurrentIndex
			{
				get { return currentIndex; }
			}

			/// <summary>
			/// 当前的值.
			/// </summary>
			public T CurrentValue
			{
				get { return list[currentIndex]; }
			}

			/// <summary>
			/// 向后移动索引, 返回false, 表示已到达数组末尾.
			/// </summary>
			/// <returns></returns>
			public bool MoveNext()
			{
				if (currentIndex == int.MinValue)
				{
					currentIndex = list.linkedListHead;
				}
				else
				{
					currentIndex = list.container[currentIndex].next;
				}

				return currentIndex >= 0;
			}

			ArrayLinkedList<T> list;
			int currentIndex;
		}

		public ArrayLinkedList(int size)
		{
			container = new ListNode[size];

			for (int i = 0; i < size; ++i)
			{
				container[i] = new ListNode();
				container[i].index = i;

				if (i < size - 1)
				{
					container[i].nextFree = i + 1;
				}
			}

			freeListHead = 0;
		}

		public int Add(T value)
		{
			int pos = PopFreeList();
			if (linkedListTail == -1)
			{
				linkedListHead = pos;
				container[pos].prev = -1;
			}
			else
			{
				container[linkedListTail].next = pos;
				container[pos].prev = linkedListTail;
			}

			container[pos].value = value;
			linkedListTail = pos;
			++Count;

			return pos;
		}

		/// <summary>
		/// 移除指定索引的节点.
		/// </summary>
		public int RemoveAt(int index)
		{
			int next = container[index].next;

			ListNode node = container[index];
			if (node.prev != -1) { container[node.prev].next = node.next; }
			if (node.next != -1) { container[node.next].prev = node.prev; }
			if (node.index == linkedListHead) { linkedListHead = node.next; }
			if (node.index == linkedListTail) { linkedListTail = node.prev; }

			PushFreeList(node);

			--Count;
			return next;
		}

		/// <summary>
		/// current索引的下一个索引.
		/// </summary>
		public int NextIndex(int current)
		{
			int answer = container[current].next;
			if (answer < 0) { answer = linkedListHead; }
			return answer;
		}

		/// <summary>
		/// current索引的上一个索引.
		/// </summary>
		public int PrevIndex(int current)
		{
			int answer = container[current].prev;
			if (answer < 0) { answer = linkedListTail; }
			return answer;
		}

		/// <summary>
		/// current索引的下一个位置的值.
		/// </summary>
		public T NextValue(int current)
		{
			return container[NextIndex(current)].value;
		}

		/// <summary>
		/// current索引的上一个位置的值.
		/// </summary>
		public T PrevValue(int current)
		{
			return container[PrevIndex(current)].value;
		}

		/// <summary>
		/// 获取index位置的值.
		/// </summary>
		public T this[int index]
		{
			get { return container[index].value; }
		}

		/// <summary>
		/// 链表的头节点的索引.
		/// </summary>
		public int First
		{
			get { return linkedListHead; }
		}

		/// <summary>
		/// 链表的尾节点的索引.
		/// </summary>
		public int Last
		{
			get { return linkedListTail; }
		}

		public int Count { get; private set; }

		public override string ToString()
		{
			string text = string.Empty;
			for (int index = First; index >= 0; index = container[index].next)
			{
				if (!string.IsNullOrEmpty(text)) { text += " "; }
				text += this[index];
			}

			return text;
		}

		public Enumerator GetEnumerator()
		{
			return new Enumerator(this);
		}

		/// <summary>
		/// 从空闲表取出一个节点, 返回该节点的索引.
		/// </summary>
		int PopFreeList()
		{
			if (freeListHead == -1) { throw new OutOfMemoryException(); }

			int answer = freeListHead;
			freeListHead = container[freeListHead].nextFree;
			container[answer].nextFree = -1;

			return answer;
		}

		/// <summary>
		/// 将节点回收到空闲表中.
		/// </summary>
		void PushFreeList(ListNode node)
		{
			node.value = default(T);
			node.prev = node.next = -1;
			node.nextFree = freeListHead;
			freeListHead = node.index;
		}

		class ListNode
		{
			public T value;

			/// <summary>
			/// 当前节点的索引.
			/// </summary>
			public int index = -1;

			/// <summary>
			/// 上一个节点的索引.
			/// </summary>
			public int prev = -1;

			/// <summary>
			/// 下一个节点的索引.
			/// </summary>
			public int next = -1;

			/// <summary>
			/// 下一个空闲节点的索引.
			/// </summary>
			public int nextFree = -1;
		}

		ListNode[] container = null;

		/// <summary>
		/// 空闲节点的链表的表头.
		/// </summary>
		int freeListHead = -1;

		/// <summary>
		/// 链表头和尾.
		/// </summary>
		int linkedListHead = -1, linkedListTail = -1;
	}
}
