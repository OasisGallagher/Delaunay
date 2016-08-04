using System;
using System.Collections.Generic;

namespace Delaunay
{
	/// <summary>
	/// ������ģ�������.
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
			/// ��ǰ����������.
			/// </summary>
			public int CurrentIndex
			{
				get { return currentIndex; }
			}

			/// <summary>
			/// ��ǰ��ֵ.
			/// </summary>
			public T CurrentValue
			{
				get { return list[currentIndex]; }
			}

			/// <summary>
			/// ����ƶ�����, ����false, ��ʾ�ѵ�������ĩβ.
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
		/// �Ƴ�ָ�������Ľڵ�.
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
		/// current��������һ������.
		/// </summary>
		public int NextIndex(int current)
		{
			int answer = container[current].next;
			if (answer < 0) { answer = linkedListHead; }
			return answer;
		}

		/// <summary>
		/// current��������һ������.
		/// </summary>
		public int PrevIndex(int current)
		{
			int answer = container[current].prev;
			if (answer < 0) { answer = linkedListTail; }
			return answer;
		}

		/// <summary>
		/// current��������һ��λ�õ�ֵ.
		/// </summary>
		public T NextValue(int current)
		{
			return container[NextIndex(current)].value;
		}

		/// <summary>
		/// current��������һ��λ�õ�ֵ.
		/// </summary>
		public T PrevValue(int current)
		{
			return container[PrevIndex(current)].value;
		}

		/// <summary>
		/// ��ȡindexλ�õ�ֵ.
		/// </summary>
		public T this[int index]
		{
			get { return container[index].value; }
		}

		/// <summary>
		/// �����ͷ�ڵ������.
		/// </summary>
		public int First
		{
			get { return linkedListHead; }
		}

		/// <summary>
		/// �����β�ڵ������.
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
		/// �ӿ��б�ȡ��һ���ڵ�, ���ظýڵ������.
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
		/// ���ڵ���յ����б���.
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
			/// ��ǰ�ڵ������.
			/// </summary>
			public int index = -1;

			/// <summary>
			/// ��һ���ڵ������.
			/// </summary>
			public int prev = -1;

			/// <summary>
			/// ��һ���ڵ������.
			/// </summary>
			public int next = -1;

			/// <summary>
			/// ��һ�����нڵ������.
			/// </summary>
			public int nextFree = -1;
		}

		ListNode[] container = null;

		/// <summary>
		/// ���нڵ������ı�ͷ.
		/// </summary>
		int freeListHead = -1;

		/// <summary>
		/// ����ͷ��β.
		/// </summary>
		int linkedListHead = -1, linkedListTail = -1;
	}
}
