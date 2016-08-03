using System;
using System.Collections.Generic;
using UnityEngine;

namespace Delaunay
{
	/// <summary>
	/// 直线, 线段的相交状态.
	/// </summary>
	public enum CrossState
	{
		/// <summary>
		/// 平行.
		/// </summary>
		Parallel,

		/// <summary>
		/// 完全重合.
		/// </summary>
		FullyOverlaps,

		/// <summary>
		/// 部分重合.
		/// </summary>
		PartiallyOverlaps,

		/// <summary>
		/// 相交, 且交点在线段上.
		/// </summary>
		CrossOnSegment,

		/// <summary>
		/// 相交, 且交点在线段的延长线上.
		/// </summary>
		CrossOnExtLine,
	}

	/// <summary>
	/// 极角比较器.
	/// </summary>
	public class PolarAngleComparer : IComparer<Vertex>
	{
		/// <summary>
		/// 构造以pivot为原点, benchmark为正方向的极角比较器. 
		/// </summary>
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
			if (MathUtility.Approximately(0, cr))
			{
				Vector3 lp = lhs.Position - pivot;
				Vector3 rp = rhs.Position - pivot;
				return lp.sqrMagnitude2().CompareTo(rp.sqrMagnitude2());
			}

			return -(int)Mathf.Sign(cr);
		}

		/// <summary>
		/// 原点.
		/// </summary>
		Vector3 pivot;

		/// <summary>
		/// 正方向向量.
		/// </summary>
		Vector3 benchmark;
	}

	/// <summary>
	/// 二元组.
	/// </summary>
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
		/// <summary>
		/// 浮点数相等比较.
		/// </summary>
		/// <todo>使用1e-6f在判断点在指向上时会有精度上的问题</todo>
		public static bool Approximately(float a, float b = 0f)
		{
			return Mathf.Abs(a - b) < 1e-5f;
		}

		/// <summary>
		/// 计算三角形(a, b, c)的外接圆心.
		/// </summary>
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

		/// <summary>
		/// 计算三角形(a, b, c)的内切圆圆心.
		/// </summary>
		/// <returns></returns>
		public static Vector3 Incentre(Vector3 a, Vector3 b, Vector3 c)
		{
			Vector3 dt1 = b - a;
			Vector3 dt2 = c - b;
			Vector3 dt3 = c - a;

			// p1和p2为两条角平分线.
			Vector3 p1 = dt1.normalized * dt3.magnitude;
			p1 = a + (p1 + dt3) / 2f;

			Vector3 p2 = dt2.normalized * dt1.magnitude;
			p2 = b + (p2 - dt1) / 2f;

			// 计算角平分线的交点.
			Vector3 center = Vector3.zero;
			GetLineCrossPoint(out center, a, p1, b, p2);
			return center;
		}

		/// <summary>
		/// 将三角形"放大", 即将每条边, 向其垂线方向平移并调整长度, 形成新的相似三角形.
		/// </summary>
		public static void Shink(Vector3[] triangle, float value)
		{
			float r = GetInscribeCircleRadius(triangle[0], triangle[1], triangle[2]);
			value /= r;

			Vector3 center = Incentre(triangle[0], triangle[1], triangle[2]);

			// 将三角形的每个顶点, 沿内切圆圆心到该顶点的向量, 平移.
			// 此时value = value_old / r, r = (center - triangle[i]).magnitude.
			// 所以下面直接乘以value即可.
			triangle[0] = triangle[0] + (center - triangle[0]) * value;
			triangle[1] = triangle[1] + (center - triangle[1]) * value;
			triangle[2] = triangle[2] + (center - triangle[2]) * value;
		}

		/// <summary>
		/// 三角形(va, vb, vc)的内切圆圆心.
		/// </summary>
		public static float GetInscribeCircleRadius(Vector3 va, Vector3 vb, Vector3 vc)
		{
			float la = (va - vb).magnitude2();
			float lb = (vb - vc).magnitude2();
			float lc = (vc - va).magnitude2();
			return Mathf.Sqrt((la + lb - lc) * (la - lb + lc) * (-la + lb + lc) / (la + lb + lc)) / 2f;
		}

		/// <summary>
		/// 计算线段(p->pd, q->qd)的相交情况.
		/// <para>如果交点o存在, 那么它必然在p->pd或其延长线上. 此时:</para> 
		/// <para>o = p + answer.x * (pd - p) = q + answer.y * (qd - q)</para>
		/// </summary>
		/// <see cref="http://stackoverflow.com/questions/563198/how-do-you-detect-where-two-line-segments-intersect"/>
		public static CrossState SegmentCross(out Vector2 answer, Vector3 p, Vector3 pd, Vector3 q, Vector3 qd)
		{
			Vector3 r = pd - p;
			Vector3 s = qd - q;
			float crs = r.cross2(s);

			answer = Vector2.zero;

			// 二者平行或共线.
			if (MathUtility.Approximately(0, crs))
			{
				bool onSeg1 = PointOnSegment(p, q, qd);
				bool onSeg2 = PointOnSegment(pd, q, qd);
				bool onSeg3 = PointOnSegment(q, p, pd);
				bool onSeg4 = PointOnSegment(qd, p, pd);

				// 二者完全重叠.
				if ((onSeg1 && onSeg2) || (onSeg3 && onSeg4))
				{
					return CrossState.FullyOverlaps;
				}

				// 二者部分重叠.
				if ((onSeg1 || onSeg2) && (onSeg3 || onSeg4))
				{
					return CrossState.PartiallyOverlaps;
				}

				// 二者平行.
				return CrossState.Parallel;
			}

			float t = (q - p).cross2(s);
			answer.Set(t / crs, (q - p).cross2(r) / crs);

			// 二者相交.
			return (InRange(answer.x) && InRange(answer.y))
			   ? CrossState.CrossOnSegment : CrossState.CrossOnExtLine;
		}

		/// <summary>
		/// 计算直线(p->pd)和(q->qd)的相交情况.
		/// </summary>
		public static CrossState GetLineCrossPoint(out Vector3 point, Vector3 p, Vector3 pd, Vector3 q, Vector3 qd)
		{
			point = Vector3.zero;

			Vector2 segCrossAnswer = Vector2.zero;
			CrossState crossState = SegmentCross(out segCrossAnswer, p, pd, q, qd);

			// 平行或重合.
			if (crossState == CrossState.Parallel || crossState == CrossState.FullyOverlaps)
			{
				return crossState;
			}

			// 交点存在, 记录在point中.
			point = p + segCrossAnswer.x * (pd - p);
			return crossState;
		}

		/// <summary>
		/// 点point是否在线段(segSrc->segDest)上.
		/// </summary>
		public static bool PointOnSegment(Vector3 point, Vector3 segSrc, Vector3 segDest)
		{
			if (!DiagonalRectContains(point, segSrc, segDest)) { return false; }
			return MathUtility.Approximately(0, point.cross2(segDest, segSrc));
		}

		/// <summary>
		/// 点point是否在由tl作为左上角, rb作为右下角的与坐标轴平行的矩形内(含在边上).
		/// </summary>
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

		/// <summary>
		/// 点point是否在由positions描述的凸多边形内.
		/// </summary>
		/// <param name="onEdge">当点在边上时的返回结果.</param>
		public static bool PolygonContains(IList<Vector3> positions, Vector3 point, bool onEdge = true)
		{
			for (int i = 1; i <= positions.Count; ++i)
			{
				Vector3 currentPosition = (i < positions.Count) ? positions[i] : positions[0];
				float cr = point.cross2(currentPosition, positions[i - 1]);
				if (MathUtility.Approximately(0f, cr) && DiagonalRectContains(point, currentPosition, positions[i - 1]))
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
		
		/// <summary>
		/// 点point到线段(segSrc->segDest)的最短距离.
		/// </summary>
		public static float MinDistance2Segment(Vector3 point, Vector3 segSrc, Vector3 segDest)
		{
			// 如果点到线段的垂足在线段上, 那么最短距离为该垂线的长度.
			// 否则, 为点到两端点的距离的最小值.
			Vector3 ray = segDest - segSrc;
			float ratio = (point - segSrc).dot2(ray) / ray.sqrMagnitude2();

			// 判断垂足是否在线段上.
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

		/// <summary>
		/// 以pivot为轴, 将src逆时针旋转radian弧度.
		/// </summary>
		public static Vector3 Rotate(Vector3 src, float radian, Vector3 pivot)
		{
			src -= pivot;
			Vector3 answer = Vector3.zero;
			answer.x = src.x * Mathf.Cos(radian) + src.z * Mathf.Sin(radian);
			answer.z = src.z * Mathf.Cos(radian) - src.x * Mathf.Sin(radian);
			return answer + pivot;
		}

		/// <summary>
		/// 点point是否在由circle为圆心, radius为半径的圆内.
		/// </summary>
		/// <param name="onCircle">当点在圆上的时返回结果.</param>
		/// <returns></returns>
		public static bool PointInCircle(Vector3 point, Vector3 center, float radius, bool onCircle = false)
		{
			radius *= radius;
			float lengthSquared = (point - center).sqrMagnitude2();
			if (MathUtility.Approximately(lengthSquared, radius))
			{
				return onCircle;
			}

			return lengthSquared < radius;
		}

		/// <summary>
		/// 判断点(point)是否在三角形(a, b, c)的外接圆内.
		/// </summary>
		/// <see cref="https://en.wikipedia.org/wiki/Circumscribed_circle#Circumcircle_equations"/>
		public static bool PointInCircumCircle(Vector3 a, Vector3 b, Vector3 c, Vector3 point)
		{
			Vector3 ba = a - b;
			Vector3 bc = c - b;
			Vector3 bv = point - b;

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

		/// <summary>
		/// 计算直线(p->dir)与平面(a, b, c)的交点.
		/// </summary>
		public static Vector3 LineCrossPlane(Vector3 a, Vector3 b, Vector3 c, Vector3 p, Vector3 dir)
		{
			Vector3 normal = Vector3.Cross(b - a, c - a);
			float t = Vector3.Dot(dir, normal);
			float d = Vector3.Dot(a - p, normal);
			Utility.Verify(!MathUtility.Approximately(0, t), "the line and plane are parallel");
			return p + dir * d / t;
		}

		/// <summary>
		/// 计算点(point)到以center为圆心, radius为半径的圆的交点.
		/// </summary>
		/// <param name="clockwise">该交点有两个, 如果该值为true, 表示, (point, center, 交点)三者为顺时针方向.</param>
		public static Vector3 GetTangent(Vector3 center, float radius, Vector3 point, bool clockwise)
		{
			float dist = (center - point).magnitude2();

			if (dist <= radius)
			{
				return point;
			}

			float r = Mathf.Acos(radius / dist);
			if (clockwise) { r = -r; }
			point = (point - center).normalized * radius;
			return MathUtility.Rotate(point, r, Vector3.zero) + center;
		}

		/// <summary>
		/// 计算圆(center1, radius1)和圆(center2, radius2)的内公切线的切点.
		/// </summary>
		public static Tuple2<Vector3, Vector3> GetInnerTangent(Vector3 center1, float radius1, Vector3 center2, float radius2, bool closewise)
		{
			float dist = (center1 - center2).magnitude2();
			Utility.Verify(dist >= (radius1 + radius2));

			if (MathUtility.Approximately(dist, radius1 + radius2))
			{
				Debug.Log("MathUtility.Approximately(dist, radius1 + radius2)");
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

		/// <summary>
		/// 计算圆(center1, radius1)和圆(center2, radius2)的外公切线的切点.
		/// </summary>
		public static Tuple2<Vector3, Vector3> GetOutterTangent(Vector3 center1, float radius1, Vector3 center2, float radius2, bool clockwise)
		{
			if (MathUtility.Approximately(radius1, radius2))
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

		/// <summary>
		/// 获取不重复的随机数.
		/// </summary>
		public static uint GetUniqueRandomInteger()
		{
			if (uniqueRandomIntegerGenerator == null)
			{
				uniqueRandomIntegerGenerator = new RandomSequenceOfUnique((uint)Time.realtimeSinceStartup, (uint)Time.realtimeSinceStartup + 1);
			}

			return uniqueRandomIntegerGenerator.next();
		}

		static RandomSequenceOfUnique uniqueRandomIntegerGenerator;

		// http://preshing.com/20121224/how-to-generate-a-sequence-of-unique-random-integers/
		class RandomSequenceOfUnique
		{
			uint m_index;
			uint m_intermediateOffset;

			static uint permuteQPR(uint x)
			{
				const uint prime = 4294967291u;
				if (x >= prime)
					return x;  // The 5 integers out of range are mapped to themselves.
				uint residue = (uint)(((ulong)x * x) % prime);
				return (x <= prime / 2) ? residue : prime - residue;
			}

			public RandomSequenceOfUnique(uint seedBase, uint seedOffset)
			{
				m_index = permuteQPR(permuteQPR(seedBase) + 0x682f0161);
				m_intermediateOffset = permuteQPR(permuteQPR(seedOffset) + 0x46790905);
			}

			public uint next()
			{
				return permuteQPR((permuteQPR(m_index++) + m_intermediateOffset) ^ 0x5bf03635);
			}
		}
	}
}
