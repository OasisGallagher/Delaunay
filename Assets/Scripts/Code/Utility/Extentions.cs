using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace Delaunay
{
	public static class Extentions
	{
		/// <summary>
		/// ��ȡ���һ��Ԫ��.
		/// </summary>
		public static T back<T>(this IList<T> target)
		{
			return target[target.Count - 1];
		}

		/// <summary>
		/// �������һ��Ԫ��.
		/// </summary>
		public static T popBack<T>(this IList<T> target)
		{
			T ans = target[target.Count - 1];
			target.RemoveAt(target.Count - 1);
			return ans;
		}

		/// <summary>
		/// ���л�Vector3.
		/// </summary>
		public static void write(this BinaryWriter writer, Vector3 v3)
		{
			writer.Write(v3.x);
			writer.Write(v3.y);
			writer.Write(v3.z);
		}

		/// <summary>
		/// �����л�Vector3.
		/// </summary>
		public static Vector3 readVector3(this BinaryReader reader)
		{
			Vector3 ans = Vector3.zero;
			ans.x = reader.ReadSingle();
			ans.y = reader.ReadSingle();
			ans.z = reader.ReadSingle();
			return ans;
		}

		/// <summary>
		/// ����2D����.
		/// </summary>
		public static float magnitude2(this Vector3 a)
		{
			a.y = 0;
			return a.magnitude;
		}

		/// <summary>
		/// ����2D����ƽ��.
		/// </summary>
		public static float sqrMagnitude2(this Vector3 a)
		{
			a.y = 0;
			return a.sqrMagnitude;
		}

		/// <summary>
		/// ����(this-p)��(b-p)��2D���ֵ.
		/// </summary>
		public static float cross2(this Vector3 a, Vector3 b, Vector3 p)
		{
			return (a - p).cross2(b - p);
		}

		/// <summary>
		/// �����b��2D���ֵ.
		/// </summary>
		public static float cross2(this Vector3 a, Vector3 b)
		{
			return a.x * b.z - a.z * b.x;
		}

		/// <summary>
		/// ����(this-p)��(b-p)��2D���ֵ.
		/// </summary>
		public static float dot2(this Vector3 a, Vector3 b, Vector3 p)
		{
			return (a - p).dot2(b - p);
		}

		/// <summary>
		/// ����this��b��2D���ֵ.
		/// </summary>
		public static float dot2(this Vector3 a, Vector3 b)
		{
			return a.x * b.x + a.z * b.z;
		}

		/// <summary>
		/// �ж�this��b��2D���Ƿ����.
		/// </summary>
		public static bool equals2(this Vector3 a, Vector3 b)
		{
			a.y = b.y = 0;
			return a == b;
		}

		/// <summary>
		/// ������b��2D�ȽϽ��.
		/// </summary>
		public static int compare2(this Vector3 a, Vector3 b)
		{
			int answer = a.x.CompareTo(b.x);
			if (answer == 0) { answer = a.z.CompareTo(b.z); }
			return answer;
		}

		/// <summary>
		/// ������2D���Ƿ��������(��Ϊnull).
		/// </summary>
		public static bool equals2(this Vertex a, Vertex b)
		{
			if (a == null) { return b == null; }
			if (b == null) { return a == null; }

			return a.Position.equals2(b.Position);
		}
	}
}
