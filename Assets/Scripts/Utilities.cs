using System;
using System.Collections.Generic;
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

	public class PolarAngleComparer : IComparer<Vertex>
	{
		public PolarAngleComparer(Vector3 pivot, Vector3 benchmark)
		{
			this.pivot = pivot;
			this.benchmark = benchmark - pivot;
		}

		public int Compare(Vertex lhs, Vertex rhs)
		{
			if (lhs == rhs) { return 0; }

			bool b1 = (lhs.Position - pivot).cross2(benchmark) > 0;
			bool b2 = (rhs.Position - pivot).cross2(benchmark) > 0;

			if (b1 != b2) { return (b2 ? -1 : 1); }

			float cr = lhs.Position.cross2(rhs.Position, pivot);
			if (Mathf.Approximately(0, cr))
			{
				Vector3 lp = lhs.Position - pivot;
				Vector3 rp = rhs.Position - pivot;
				return lp.sqrMagnitude2().CompareTo(rp.sqrMagnitude2());
			}

			return -(int)Mathf.Sign(cr);
		}

		Vector3 pivot;
		Vector3 benchmark;
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

		public static bool PolygonContains(IList<Vector3> positions, Vector3 point, bool onEdge = true)
		{
			for (int i = 1; i <= positions.Count; ++i)
			{
				Vector3 currentPosition = (i < positions.Count) ? positions[i] : positions[0];
				float cr = point.cross2(currentPosition, positions[i - 1]);
				if (Mathf.Approximately(0f, cr))
				{
					return onEdge;
				}

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

		public static Vector3 Rotate(Vector3 src, float radian, Vector3 pivot)
		{
			src -= pivot;
			Vector3 answer = Vector3.zero;
			answer.x = src.x * Mathf.Cos(radian) + src.z * Mathf.Sin(radian);
			answer.z = src.z * Mathf.Cos(radian) - src.x * Mathf.Sin(radian);
			return answer + pivot;
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

	
}
