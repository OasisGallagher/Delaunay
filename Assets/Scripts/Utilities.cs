using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

namespace Delaunay
{
	public enum LineCrossState
	{
		Parallel,
		FullyOverlaps,
		PartiallyOverlaps,
		CrossOnSegment,
		CrossOnExtLine,
	}

	public class VertexComparer : IComparer<Vertex>
	{
		public int Compare(Vertex lhs, Vertex rhs)
		{
			return lhs.Position.compare2(rhs.Position);
		}
	}

	public class Tuple2<T1, T2>
	{
		public Tuple2(T1 first, T2 second)
		{
			this.First = first;
			this.Second = second;
		}

		public Tuple2() { }

		public T1 First;
		public T2 Second;
	}

	public static class Utility
	{
		/*public static void FixVertexHalfEdge(Vertex vertex)
		{
			if (vertex.Edge != null)
			{
				vertex.Edge = vertex.Edge.Pair.Next;
			}
		}*/

		public static HalfEdge CycleLink(HalfEdge x, HalfEdge y, HalfEdge z)
		{
			Verify(x != null && y != null && z != null);

			x.Next = y;
			y.Next = z;
			z.Next = x;
			return x;
		}

		public static HalfEdge GetHalfEdgeByDirection(Triangle triangle, int direction)
		{
			direction = Mathf.Abs(direction);
			Verify(direction >= 1 && direction <= 3);
			return (direction == 1) ? triangle.AB : (direction == 2 ? triangle.BC : triangle.CA);
		}

		public static LineCrossState SegmentCross(out Vector2 answer, Vector3 p, Vector3 pd, Vector3 q, Vector3 qd)
		{
			Vector3 r = pd - p;
			Vector3 s = qd - q;
			float crs = r.cross2(s);

			answer = Vector2.zero;

			if (Mathf.Approximately(0, crs))
			{
				bool onSeg1 = PointOnSegment(p, q, qd);
				bool onSeg2 = PointOnSegment(pd, q, qd);
				bool onSeg3 = PointOnSegment(q, p, pd);
				bool onSeg4 = PointOnSegment(qd, p, pd);
				if((onSeg1 && onSeg2) || (onSeg3 && onSeg4))
				{
					return LineCrossState.FullyOverlaps;
				}
				
				if ((onSeg1 || onSeg2) && (onSeg3 || onSeg4))
				{
					return LineCrossState.PartiallyOverlaps;
				}
				
				return LineCrossState.Parallel;
			}

			float t = (q - p).cross2(s);
			answer.Set(t / crs, (q - p).cross2(r) / crs);

			return (InRange(answer.x) && InRange(answer.y))
			   ? LineCrossState.CrossOnSegment : LineCrossState.CrossOnExtLine;
		}

		public static LineCrossState GetLineCrossPoint(out Vector3 point, Vector3 p, Vector3 pd, Vector3 q, Vector3 qd)
		{
			point = Vector3.zero;

			Vector2 segCrossAnswer = Vector2.zero;
			LineCrossState crossState = SegmentCross(out segCrossAnswer, p, pd, q, qd);

			if (crossState == LineCrossState.Parallel || crossState == LineCrossState.FullyOverlaps)
			{
				return crossState;
			}

			point = p + segCrossAnswer.x * (pd - p);
			return crossState;
		}

		public static bool PointOnSegment(Vector3 point, Vector3 segSrc, Vector3 segDest)
		{
			if (!DiagonalRectContains(point, segSrc, segDest)) { return false; }
			return Mathf.Approximately(0, point.cross2(segDest, segSrc));
		}

		public static bool DiagonalRectContains(Vector3 point, Vector3 tl, Vector3 rb)
		{
			float xMin = tl.x, xMax = rb.x;
			if (xMin > xMax) { float tmp = xMin; xMin = xMax; xMax = tmp; }

			float zMin = tl.z, zMax = rb.z;
			if (zMin > zMax) { float tmp = zMin; zMin = zMax; zMax = tmp; }

			return point.x >= xMin && point.x <= xMax && point.z >= zMin && point.z <= zMax;
		}

		public static bool InRange(float f, float a = 0f, float b = 1f)
		{
			return f >= a && f <= b;
		}

		public static bool PolygonContains(IList<Vector3> positions, Vector3 point)
		{
			for (int i = 1; i <= positions.Count; ++i)
			{
				Vector3 currentPosition = (i < positions.Count) ? positions[i] : positions[0];
				if (point.cross2(currentPosition, positions[i - 1]) > 0)
				{
					return false;
				}
			}

			return true;
		}

		public static float MinDistance(Vector3 point, Vector3 segSrc, Vector3 segDest)
		{
			Vector3 ray = segDest - segSrc;
			float ratio = (point - segSrc).dot2(ray) / ray.sqrMagnitude2();

			if (ratio < 0f)
			{
				ratio = 0f;
			}
			else if (ratio > 1f)
			{
				ratio = 1f;
			}

			return (segSrc + ratio * ray - point).magnitude2();
		}

		public static bool PointInCircumCircle(Vertex a, Vertex b, Vertex c, Vertex v)
		{
			// https://en.wikipedia.org/wiki/Circumscribed_circle#Circumcircle_equations
			Vector3 ba = a.Position - b.Position;
			Vector3 bc = c.Position - b.Position;
			Vector3 bv = v.Position - b.Position;

			float sqrBa = ba.x * ba.x + ba.z * ba.z;
			float sqrBc = bc.x * bc.x + bc.z * bc.z;
			float sqrBv = bv.x * bv.x + bv.z * bv.z;

			/*
			 *		 bax bay baSqr
			 * det | bcx bcy bcSqr | <= 0.
			 *		 bvx bvy bvSqr
			 */

			float a1 = ba.x, b1 = ba.z, c1 = sqrBa;
			float a2 = bc.x, b2 = bc.z, c2 = sqrBc;
			float a3 = bv.x, b3 = bv.z, c3 = sqrBv;

			float det = a1 * (b2 * c3 - b3 * c2) + a2 * (b3 * c1 - b1 * c3) + a3 * (b1 * c2 - b2 * c1);

			return det > 0;
		}

		public static bool Assert(bool condition)
		{
			return Assert(condition, "Verify failed");
		}

		public static bool Assert(bool condition, string message, params object[] arguments)
		{
			if (!condition)
			{
#if DEBUG
				throw new Exception(string.Format(message ?? "Condition failed", arguments));
#else
				Debug.LogError(string.Format(message ?? "Condition failed", arguments));
#endif
			}
			return condition;
		}

		public static bool Verify(bool condition)
		{
			return Verify(condition, "Verify failed");
		}

		public static bool Verify(bool condition, string message, params object[] arguments)
		{
			if (!condition)
			{
				throw new Exception(string.Format(message ?? "Condition failed", arguments));
			}

			return condition;
		}
	}

	public class XmlWriterScope : IDisposable
	{
		XmlWriter writer = null;
		public XmlWriterScope(XmlWriter writer, string localName)
		{
			this.writer = writer;
			writer.WriteStartElement(localName);
		}

		public void Dispose() { writer.WriteEndElement(); }
	}

	public static class Extentions
	{
		public static T back<T>(this IList<T> target)
		{
			return target[target.Count - 1];
		}

		public static IList<T2> transform<T1, T2>(this IList<T1> src, IList<T2> dest, Func<T1, T2> func)
		{
			for (int i = 0; i < dest.Count; ++i)
			{
				dest[i] = func(src[i]);
			}

			return dest;
		}

		public static float magnitude2(this Vector3 a)
		{
			a.y = 0;
			return a.magnitude;
		}

		public static float sqrMagnitude2(this Vector3 a)
		{
			a.y = 0;
			return a.sqrMagnitude;
		}

		public static float cross2(this Vector3 a, Vector3 b, Vector3 p)
		{
			return (a - p).cross2(b - p);
		}

		public static float cross2(this Vector3 a, Vector3 b)
		{
			return a.x * b.z - a.z * b.x;
		}

		public static float dot2(this Vector3 a, Vector3 b, Vector3 p)
		{
			return (a - p).dot2(b - p);
		}

		public static float dot2(this Vector3 a, Vector3 b)
		{
			return a.x * b.x + a.z * b.z;
		}

		public static bool equals2(this Vector3 a, Vector3 b)
		{
			a.y = b.y = 0;
			return a == b;
		}

		public static int compare2(this Vector3 a, Vector3 b)
		{
			int answer = a.x.CompareTo(b.x);
			if (answer == 0) { answer = a.z.CompareTo(b.z); }
			return answer;
		}

		public static bool equals2(this Vertex a, Vertex b)
		{
			if (a == null) { return b == null; }
			if (b == null) { return a == null; }

			return a.Position.equals2(b.Position);
		}
	}

	public static class ConvexHullComputer
	{
		class VertexStack
		{
			List<Vector3> container = new List<Vector3>();
			public Vector3 Pop()
			{
				Vector3 result = container[container.Count - 1];
				container.RemoveAt(container.Count - 1);
				return result;
			}

			public void Push(Vector3 u)
			{
				container.Add(u);
			}

			public Vector3 Peek(int index = 0)
			{
				return container[container.Count - 1 - index];
			}

			public List<Vector3> Container { get { return new List<Vector3>(container); } }
		}

		static Vector3 PopLowestVertex(List<Vector3> vertices)
		{
			float minZ = vertices[0].z;
			int index = 0;
			for (int i = 1; i < vertices.Count; ++i)
			{
				if (vertices[i].z < minZ)
				{
					index = i;
					minZ = vertices[i].z;
				}
			}

			Vector3 result = vertices[index];
			vertices[index] = vertices[vertices.Count - 1];
			vertices.RemoveAt(vertices.Count - 1);
			return result;
		}

		class ConvexHullVertexComparer : IComparer<Vector3>
		{
			Vector3 start = Vector3.zero;
			public ConvexHullVertexComparer(Vector3 o)
			{
				start = o;
			}

			int IComparer<Vector3>.Compare(Vector3 lhs, Vector3 rhs)
			{
				bool b1 = (lhs - start).cross2(new Vector3(1, 0, 0)) > 0;
				bool b2 = (rhs - start).cross2(new Vector3(1, 0, 0)) > 0;

				if (b1 != b2) { return b2 ? -1 : 1; }

				float c = (lhs - start).cross2(rhs - start);
				if (!Mathf.Approximately(c, 0)) { return c > 0 ? -1 : 1; }

				Vector3 drhs = rhs - start;
				Vector3 dlhs = lhs - start;
				drhs.y = dlhs.y = 0f;

				return Math.Sign(drhs.sqrMagnitude2() - dlhs.sqrMagnitude2());
			}
		}

		public static List<Vector3> Compute(List<Vector3> vertices)
		{
			if (vertices.Count <= 3) { return vertices; }

			Vector3 p0 = PopLowestVertex(vertices);
			vertices.Sort(new ConvexHullVertexComparer(p0));

			VertexStack stack = new VertexStack();
			stack.Push(p0);
			stack.Push(vertices[0]);
			stack.Push(vertices[1]);

			for (int i = 2; i < vertices.Count; ++i)
			{
				Vector3 pi = vertices[i];
				for (; ; )
				{
					Utility.Verify(stack.Container.Count > 0);

					Vector3 top = stack.Peek(), next2top = stack.Peek(1);
					float cr = (next2top - top).cross2(pi - top);

					if (cr < 0) { break; }

					stack.Pop();
				}

				stack.Push(pi);
			}

			return stack.Container;
		}
	}

	public class ArrayLinkedList<T>
	{
		public class IndexEnumerator
		{
			public IndexEnumerator(ArrayLinkedList<T> list)
			{
				this.list = list;
			}

			public int ListIndex
			{
				get { return currentIndex; }
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

		public IndexEnumerator GetIndexEnumerator()
		{
			return new IndexEnumerator(this);
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

	static class EditorConstants
	{
		public const string kXmlVertex = "Vertex";
		public const string kXmlEdge = "Edge";
		public const string kXmlTriangle = "Triangle";

		public const string kXmlRoot = "Root";
		public const string kXmlAllVertices = "AllVertices";
		public const string kXmlAllEdges = "AllEdges";
		public const string kXmlAllTriangles = "AllTriangles";

		public const float kPanelWidth = 60;
		public const float kConvexHullGizmosHeight = 0.7f;
		public const float kNeighborTriangleGizmosHeight = 0.5f;

		public const int kMaxStackCapacity = 4096;
		public const int kDebugInvalidCycle = 32;

		public static readonly Vector3 kPathRendererOffset = new Vector3(0, 0.42f, 0);
		public static readonly Vector3 kTriangleGizmosOffset = new Vector3(0, 0.4f, 0);
		public static readonly Vector3 kTriangleMeshOffset = new Vector3(0, 0.1f, 0);
		public static readonly Vector3 kHalfEdgeGizmosOffset = new Vector3(0, 0.3f, 0);
		public static readonly Vector3 kEdgeGizmosOffset = new Vector3(0, 0.3f, 0);
		public static readonly int[] kTriangleIndices = new int[] { 0, 2, 1 };
		public static readonly Vector2[] kUV = new Vector2[] { Vector2.zero, Vector2.zero, Vector2.zero };

		public static readonly VertexComparer kVertexComparer = new VertexComparer();

		public static readonly Material kWalkableMaterial = (Material)Resources.Load("Materials/Walkable");
		public static readonly Material kBlockMaterial = (Material)Resources.Load("Materials/Block");
	}
}
