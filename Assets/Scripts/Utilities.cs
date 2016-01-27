﻿using System;
using System.Collections.Generic;
using UnityEngine;

namespace Delaunay
{
	public enum LineCrossState
	{
		Parallel,
		Colinear,
		CrossOnSegment,
		CrossOnExtLine,
	}

	public static class Utility
	{
		public static void FixVertexHalfEdge(Vertex vertex)
		{
			if (vertex.Edge != null)
			{
				vertex.Edge = vertex.Edge.Pair.Next;
			}
		}

		public static HalfEdge CycleLink(HalfEdge x, HalfEdge y, HalfEdge z)
		{
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

		public static Vector2? SegmentCross(Vector3 p, Vector3 pd, Vector3 q, Vector3 qd)
		{
			Vector3 r = pd - p;
			Vector3 s = qd - q;
			float crs = Cross2D(r, s);

			if (Mathf.Approximately(0, crs))
			{
				return null;
			}

			float t = Cross2D(q - p, s) / crs;
			float u = Cross2D(q - p, r) / crs;

			return new Vector2(t, u);
		}

		public static LineCrossState GetLineCrossPoint(out Vector3 point, Vector3 p, Vector3 pd, Vector3 q, Vector3 qd)
		{
			Vector2? answer = SegmentCross(p, pd, q, qd);
			point = Vector3.zero;

			if (!answer.HasValue)
			{
				return LineCrossState.Parallel;
			}

			if (Mathf.Approximately(0, answer.Value.x) || Mathf.Approximately(0, answer.Value.y))
			{
				return LineCrossState.Colinear;
			}

			point = p + answer.Value.x * (pd - p);
			return (InRange(answer.Value.x) && InRange(answer.Value.y))
				? LineCrossState.CrossOnSegment : LineCrossState.CrossOnExtLine;
		}

		public static bool PointOnSegment(Vector3 point, Vector3 segSrc, Vector3 segDest)
		{
			if (!InDiagonalRectangle(point, segSrc, segDest)) { return false; }
			return Mathf.Approximately(0, Cross2D(point - segSrc, segDest - segSrc));
		}

		public static bool InDiagonalRectangle(Vector3 point, Vector3 tl, Vector3 rb)
		{
			float xMin = tl.x, xMax = rb.x;
			if (xMin > xMax) { float tmp = xMin; xMin = xMax; xMax = tmp; }

			float zMin = tl.z, zMax = rb.z;
			if (zMin > zMax) { float tmp = zMin; zMin = zMax; zMax = tmp; }

			return point.x > xMin && point.x < xMax && point.z > zMin && point.z < zMax;
		}

		public static float Cross2D(Vector3 a, Vector3 b)
		{
			return a.x * b.z - a.z * b.x;
		}

		public static float Cross2D(Vector3 a, Vector3 b, Vector3 pivot)
		{
			return Cross2D(a - pivot, b - pivot);
		}

		public static bool Equals2D(Vertex a, Vertex b)
		{
			if (a == null) { return b == null; }
			if (b == null) { return a == null; }

			return Equals2D(a.Position, b.Position);
		}

		public static bool Equals2D(Vector3 a, Vector3 b)
		{
			a.y = b.y = 0;
			return a == b;
		}

		public static bool InRange(float f, float a = 0f, float b = 1f)
		{
			return f > a && f < b;
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

		public static bool Verify(bool condition)
		{
			return Verify(condition, "Verify failed");
		}

		public static bool Verify(bool condition, string message, params object[] arguments)
		{
			if (!condition)
			{
				Debug.LogError(string.Format(message ?? "Condition failed", arguments));
				Debug.DebugBreak();
			}

			return condition;
		}
	}

	public static class Extentions
	{
		public static T Back<T>(this List<T> target)
		{
			return target[target.Count - 1];
		}
	}

	public static class ConvexHullComputer
	{
		class VertexStack
		{
			List<Vertex> container = new List<Vertex>();
			public Vertex Pop()
			{
				Vertex result = container[container.Count - 1];
				container.RemoveAt(container.Count - 1);
				return result;
			}

			public void Push(Vertex u)
			{
				container.Add(u);
			}

			public Vertex Peek(int index = 0)
			{
				return container[container.Count - 1 - index];
			}

			public List<Vertex> Container { get { return new List<Vertex>(container); } }
		}

		static Vertex PopLowestVertex(List<Vertex> vertices)
		{
			float minZ = vertices[0].Position.z;
			int index = 0;
			for (int i = 1; i < vertices.Count; ++i)
			{
				if (vertices[i].Position.z < minZ)
				{
					index = i;
					minZ = vertices[i].Position.z;
				}
			}

			Vertex result = vertices[index];
			vertices[index] = vertices[vertices.Count - 1];
			vertices.RemoveAt(vertices.Count - 1);
			return result;
		}

		class VertexComparer : IComparer<Vertex>
		{
			Vertex start = null;
			public VertexComparer(Vertex o)
			{
				start = o;
			}

			int IComparer<Vertex>.Compare(Vertex lhs, Vertex rhs)
			{
				bool b1 = Utility.Cross2D(lhs.Position - start.Position, new Vector3(1, 0, 0)) > 0;
				bool b2 = Utility.Cross2D(rhs.Position - start.Position, new Vector3(1, 0, 0)) > 0;

				if (b1 != b2) { return b2 ? -1 : 1; }

				float c = Utility.Cross2D(lhs.Position - start.Position, rhs.Position - start.Position);
				if (!Mathf.Approximately(c, 0)) { return c > 0 ? -1 : 1; }

				// 对于极角相等的两个点, 按照到start的距离递减的顺序排列.
				// 从而在Unique中删除.
				Vector3 drhs = rhs.Position - start.Position;
				Vector3 dlhs = lhs.Position - start.Position;
				drhs.y = dlhs.y = 0f;

				return Math.Sign(drhs.sqrMagnitude - dlhs.sqrMagnitude);
			}
		}

		public static List<Vertex> Compute(List<Vertex> vertices)
		{
			if (vertices.Count <= 3) { return vertices; }

			Vertex p0 = PopLowestVertex(vertices);
			vertices.Sort(new VertexComparer(p0));

			VertexStack stack = new VertexStack();
			stack.Push(p0);
			stack.Push(vertices[0]);
			stack.Push(vertices[1]);

			for (int i = 2; i < vertices.Count; ++i)
			{
				Vertex pi = vertices[i];
				for (; ; )
				{
					Utility.Verify(stack.Container.Count > 0);

					Vertex top = stack.Peek(), next2top = stack.Peek(1);
					// next2top -> top -> pi is a non-left turn.
					float cr = Utility.Cross2D(
						next2top.Position - top.Position,
						pi.Position - top.Position
					);

					if (cr < 0) { break; }

					stack.Pop();
				}

				stack.Push(pi);
			}

			return stack.Container;
		}
	}
	
	static class EditorParameter
	{
		public static bool plant = true;
	}

	static class EditorConstants
	{
		public const float kPanelWidth = 60;
		public const float kConvexHullGizmosHeight = 0.7f;
		public const float kTriangleGizmosHeight = 0.1f;
		public const float kNeighborTriangleGizmosHeight = 0.5f;

		public const int kMaxStackCapacity = 4096;
		public const int kDebugInvalidCycle = 32;

		public static readonly Vector3 kTriangleMeshOffset = new Vector3(0, 0.1f, 0);
		public static readonly Vector3 kHalfEdgeGizmosOffset = new Vector3(0, 0.3f, 0);
		public static readonly Vector3 kEdgeGizmosOffset = new Vector3(0, 0.2f, 0);
		public static readonly int[] kTriangleIndices = new int[] { 0, 2, 1 };
		public static readonly Vector2[] kUV = new Vector2[] { Vector2.zero, Vector2.zero, Vector2.zero };

		public static readonly Material kWalkableMaterial = (Material)Resources.Load("Materials/Walkable");
		public static readonly Material kBlockMaterial = (Material)Resources.Load("Materials/Block");
	}
}