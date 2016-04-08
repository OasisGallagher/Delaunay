using System;
using System.Collections.Generic;
using UnityEngine;

namespace Delaunay
{
	public enum CrossState
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

	public struct Tuple2<T1, T2>
	{
		public Tuple2(T1 first, T2 second)
		{
			this.First = first;
			this.Second = second;
		}

		public void Set(T1 first, T2 second)
		{
			this.First = first;
			this.Second = second;
		}

		public T1 First;
		public T2 Second;
	}

	public static class Utility
	{
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

	public static class MathUtility
	{
		public static void DrawGizmosCircle(Vector3 center, float radius, Color color, float y = 0f)
		{
			Color oldColor = Gizmos.color;
			Gizmos.color = color;
			Vector3 from = new Vector3(center.x + radius, y, center.z);
			for (float i = 1; i < 360; ++i)
			{
				float radian = Mathf.Deg2Rad * i;
				float x = Mathf.Cos(radian) * radius + center.x;
				float z = Mathf.Sin(radian) * radius + center.z;
				Vector3 to = new Vector3(x, y, z);
				Gizmos.DrawLine(from, to);
				from = to;
			}
			Gizmos.color = oldColor;
		}

		public static Vector3 Centroid(Vector3[] a)
		{
			return Centroid(a[0], a[1], a[2]);
		}

		public static Vector3 Centroid(Vector3 a, Vector3 b, Vector3 c)
		{
			return (a + b + c) / 3f;
		}

		public static Vector3 Circumcentre(Vector3 a, Vector3 b, Vector3 c)
		{
			float t1 = a.x * a.x + a.z * a.z;
			float t2 = b.x * b.x + b.z * b.z;
			float t3 = c.x * c.x + c.z * c.z;
			float tmp = a.x * b.z + b.x * c.z + c.x * a.z - a.x * c.z - b.x * a.z - c.x * b.z;
			tmp *= 2f;

			float x = (t2 * c.z + t1 * b.z + t3 * a.z - t2 * a.z - t3 * b.z - t1 * c.z) / tmp;
			float z = (t3 * b.x + t2 * a.x + t1 * c.x - t1 * b.x - t2 * c.x - t3 * a.x) / tmp;
			return new Vector3(x, (a.y + b.y + c.y) / 3f, z);
		}

		public static Vector3 Incentre(Vector3 a, Vector3 b, Vector3 c)
		{
			Vector3 dt1 = b - a;
			Vector3 dt2 = c - b;
			Vector3 dt3 = c - a;

			Vector3 p1 = dt1.normalized * dt3.magnitude;
			p1 = a + (p1 + dt3) / 2f;

			Vector3 p2 = dt2.normalized * dt1.magnitude;
			p2 = b + (p2 - dt1) / 2f;

			Vector3 center = Vector3.zero;
			GetLineCrossPoint(out center, a, p1, b, p2);
			return center;
		}

		public static void Shink(Vector3[] triangle, float value)
		{
			float r = GetInscribeCircleRadius(triangle[0], triangle[1], triangle[2]);
			value /= r;

			Vector3 center = Incentre(triangle[0], triangle[1], triangle[2]);

			triangle[0] = triangle[0] + (center - triangle[0]) * value;
			triangle[1] = triangle[1] + (center - triangle[1]) * value;
			triangle[2] = triangle[2] + (center - triangle[2]) * value;
		}

		public static Vector3 Nearest(Vector3 position, params Vector3[] list)
		{
			Vector3 answer = position;
			float minSqrDist = float.PositiveInfinity;
			
			foreach (Vector3 item in list)
			{
				float sqrDist = (item - position).sqrMagnitude2();
				if (sqrDist < minSqrDist)
				{
					answer = item;
					minSqrDist = sqrDist;
				}
			}

			return answer;
		}

		public static float GetInscribeCircleRadius(Vector3 va, Vector3 vb, Vector3 vc)
		{
			float la = (va - vb).magnitude2();
			float lb = (vb - vc).magnitude2();
			float lc = (vc - va).magnitude2();
			return Mathf.Sqrt((la + lb - lc) * (la - lb + lc) * (-la + lb + lc) / (la + lb + lc)) / 2f;
		}

		public static bool Place(out Vector3 answer, Vector3 va, Vector3 vb, Vector3 vc, Vector3 reference, float radius)
		{
			answer = Vector3.zero;

			if (GetInscribeCircleRadius(va, vb, vc) < radius)
			{
				return false;
			}

			Vector3 nearestVertex = MathUtility.Nearest(reference, va, vb, vc);
			Vector3 other1 = va, other2 = vb;
			if (nearestVertex == va)
			{
				other1 = vb;
				other2 = vc;
			}
			else if (nearestVertex == vb)
			{
				other1 = va;
				other2 = vc;
			}

			float magnitude = (other1 - nearestVertex).magnitude2() * (other2 - nearestVertex).magnitude2();
			float radian = other1.cross2(other2, nearestVertex) / magnitude;

			radian = Mathf.Asin(radian) / 2f;
			magnitude = Mathf.Abs(radius / Mathf.Sin(radian));

			answer = (other1 - nearestVertex).normalized * magnitude;
			answer = Rotate(answer, -radian, Vector3.zero) + nearestVertex;

			return true;
		}

		public static CrossState SegmentCross(out Vector2 answer, Vector3 p, Vector3 pd, Vector3 q, Vector3 qd)
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
				if ((onSeg1 && onSeg2) || (onSeg3 && onSeg4))
				{
					return CrossState.FullyOverlaps;
				}

				if ((onSeg1 || onSeg2) && (onSeg3 || onSeg4))
				{
					return CrossState.PartiallyOverlaps;
				}

				return CrossState.Parallel;
			}

			float t = (q - p).cross2(s);
			answer.Set(t / crs, (q - p).cross2(r) / crs);

			return (InRange(answer.x) && InRange(answer.y))
			   ? CrossState.CrossOnSegment : CrossState.CrossOnExtLine;
		}

		public static CrossState GetLineCrossPoint(out Vector3 point, Vector3 p, Vector3 pd, Vector3 q, Vector3 qd)
		{
			point = Vector3.zero;

			Vector2 segCrossAnswer = Vector2.zero;
			CrossState crossState = SegmentCross(out segCrossAnswer, p, pd, q, qd);

			if (crossState == CrossState.Parallel || crossState == CrossState.FullyOverlaps)
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
				if (Mathf.Approximately(0f, cr) && DiagonalRectContains(point, currentPosition, positions[i - 1]))
				{
					return onEdge;
				}

				if (cr > 0)
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

		public static bool PointInCircumCircle(Vector3 a, Vector3 b, Vector3 c, Vector3 v)
		{
			// https://en.wikipedia.org/wiki/Circumscribed_circle#Circumcircle_equations
			Vector3 ba = a - b;
			Vector3 bc = c - b;
			Vector3 bv = v - b;

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

		public static Vector3 LineCrossPlane(Vector3 a, Vector3 b, Vector3 c, Vector3 p, Vector3 dir)
		{
			Vector3 normal = Vector3.Cross(b - a, c - a);
			float t = Vector3.Dot(dir, normal);
			float d = Vector3.Dot(a - p, normal);
			Verify(!Mathf.Approximately(0, t), "the line and plane are parallel");
			return p + dir * d / t;
		}

		public static Vector3 GetTangent(Vector3 center, float radius, Vector3 point, bool clockwise)
		{
			float dist = (center - point).magnitude2();
			Utility.Verify(dist >= radius);

			if (Mathf.Approximately(dist, radius))
			{
				return point;
			}

			float r = Mathf.Acos(radius / dist);
			if (clockwise) { r = -r; }
			point = (point - center).normalized * radius;
			return MathUtility.Rotate(point, r, Vector3.zero) + center;
		}

		public static Tuple2<Vector3, Vector3> GetInnerTangent(Vector3 center1, float radius1, Vector3 center2, float radius2, bool closewise)
		{
			float dist = (center1 - center2).magnitude2();
			Utility.Verify(dist >= (radius1 + radius2));

			if (Mathf.Approximately(dist, radius1 + radius2))
			{
				Vector3 point = (center2 - center1).normalized * radius1 + center1;
				return new Tuple2<Vector3, Vector3>(point, point);
			}

			float d = radius1 * dist / (radius1 + radius2);
			Vector3 ray = center2 - center1;
			ray = ray.normalized * d;
			ray += center1;

			return new Tuple2<Vector3, Vector3>(
				GetTangent(center1, radius1, ray, closewise),
				GetTangent(center2, radius2, ray, closewise)
			);
		}

		public static Tuple2<Vector3, Vector3> GetOutterTangent(Vector3 center1, float radius1, Vector3 center2, float radius2, bool clockwise)
		{
			if (Mathf.Approximately(radius1, radius2))
			{
				Vector3 d = center2 - center1;
				d = d.normalized * radius1;
				float radian = Mathf.PI / 2f;
				if (!clockwise) { radian = -radian; }
				Vector3 rotated = MathUtility.Rotate(d, radian, Vector3.zero);
				return new Tuple2<Vector3, Vector3>(rotated + center1, rotated + center2);
			}

			float dist = (center1 - center2).magnitude2();
			Utility.Verify(dist >= (radius1 + radius2));

			dist = dist / Mathf.Abs(radius1 - radius2);
			Vector3 ray = center1 - center2;

			ray = ray.normalized * dist;
			if (radius1 > radius2)
			{
				ray = -ray;
				ray += center2;
			}
			else
			{
				ray += center1;
			}

			return new Tuple2<Vector3, Vector3>(
				GetTangent(center1, radius1, ray, clockwise),
				GetTangent(center2, radius2, ray, clockwise)
			);
		}
	}
}
