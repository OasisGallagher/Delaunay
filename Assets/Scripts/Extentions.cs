using System;
using System.Collections.Generic;
using UnityEngine;
namespace Delaunay
{
	public static class Extentions
	{
		public static T back<T>(this IList<T> target)
		{
			return target[target.Count - 1];
		}

		public static T extremum<T>(this IList<T> target, IComparer<T> comparer)
		{
			T answer = target[0];
			for (int i = 1; i < target.Count; ++i)
			{
				if (comparer.Compare(answer, target[i]) < 0)
				{
					answer = target[i];
				}
			}

			return answer;
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
}
