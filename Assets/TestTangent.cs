using UnityEngine;
using System.Collections;

namespace Delaunay
{
	public class TestTangent : MonoBehaviour
	{
		public Vector3 Center1;
		public float Radius1 = 1;

		public Vector3 Center2 = new Vector3(5, 0, 0);
		public float Radius2 = 3;

		public Vector3 Point;

		void OnDrawGizmos()
		{
			DrawCircle(Center1, Radius1);
			DrawCircle(Center2, Radius2);
			Tuple2<Vector3, Vector3> tuple2 = GetInnerTangent(Center1, Radius1, Center2, Radius2);
			Gizmos.DrawLine(tuple2.First, tuple2.Second);
		}

		static Vector3 GetTangent(Vector3 center, float radius, Vector3 point, bool clockwise)
		{
			float r = Mathf.Acos(radius / (center - point).magnitude2());
			if (!clockwise) { r = -r; }
			point = (point - center).normalized * radius;
			return Utility.Rotate(point, r, Vector3.zero) + center;
		}

		static Tuple2<Vector3, Vector3> GetInnerTangent(Vector3 center1, float radius1, Vector3 center2, float radius2)
		{
			float dist = (center1 - center2).magnitude2();
			float d = radius1 * dist / (radius1 + radius2);
			Vector3 ray = center2 - center1;
			ray = ray.normalized * d;
			ray += center1;
			return new Tuple2<Vector3, Vector3>(
				GetTangent(center1, radius1, ray, false),
				GetTangent(center2, radius2, ray, true)
			);
		}

		static Tuple2<Vector3, Vector3> GetOutterTangent(Vector3 center1, float radius1, Vector3 center2, float radius2)
		{
			float dist = (center1 - center2).magnitude2();
			dist = dist / Mathf.Abs(radius1 - radius2);
			Vector3 ray = center1 - center2;

			ray = ray.normalized * dist;
			if (radius1 > radius2)
			{
				ray = -ray;
				ray += center1;
			}
			else
			{
				ray += center2;
			}

			return new Tuple2<Vector3, Vector3>(
				GetTangent(center1, radius1, ray, true),
				GetTangent(center2, radius2, ray, true)
			);
		}

		static void DrawCircle(Vector3 center, float radius)
		{
			Vector3 from = new Vector3(center.x + radius, 0, center.z);
			for (float i = 1; i < 360; ++i)
			{
				float radian = Mathf.Deg2Rad * i;
				float x = Mathf.Cos(radian) * radius + center.x;
				float z = Mathf.Sin(radian) * radius + center.z;
				Vector3 to = new Vector3(x, 0, z);
				Gizmos.DrawLine(from, to);
				from = to;
			}
		}
	}
}