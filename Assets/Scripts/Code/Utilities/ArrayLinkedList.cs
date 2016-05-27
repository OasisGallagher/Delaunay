using System;
using System.Collections.Generic;

namespace Delaunay
{
	public class ArrayLinkedList<T>
	{
		public class ALEnumerator
		{
			public ALEnumerator(ArrayLinkedList<T> list)
			{
				this.list = list;
			}

			public int CurrentIndex
			{
				get { return currentIndex; }
			}

			public T CurrentValue
			{
				get { return list[currentIndex]; }
			}

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

			ArrayLinkedList<T> list = null;
			int currentIndex = int.MinValue;
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

		public int NextIndex(int current)
		{
			int answer = container[current].next;
			if (answer < 0) { answer = linkedListHead; }
			return answer;
		}

		public int PrevIndex(int current)
		{
			int answer = container[current].prev;
			if (answer < 0) { answer = linkedListTail; }
			return answer;
		}

		public T NextValue(int current)
		{
			return container[NextIndex(current)].value;
		}

		public T PrevValue(int current)
		{
			return container[PrevIndex(current)].value;
		}

		public T this[int index]
		{
			get { return container[index].value; }
		}

		public int First
		{
			get { return linkedListHead; }
		}

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

		public ALEnumerator GetEnumerator()
		{
			return new ALEnumerator(this);
		}

		int PopFreeList()
		{
			if (freeListHead == -1) { throw new OutOfMemoryException(); }

			int answer = freeListHead;
			freeListHead = container[freeListHead].nextFree;
			container[answer].nextFree = -1;

			return answer;
		}

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
			public int index = -1;
			public int prev = -1;
			public int next = -1;
			public int nextFree = -1;
		}

		ListNode[] container = null;
		int freeListHead = -1;
		int linkedListHead = -1, linkedListTail = -1;
	}
}
