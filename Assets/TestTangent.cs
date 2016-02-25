using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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
			Tuple2<Vector3, Vector3> tuple2 = MathUtility.GetOutterTangent(Center1, Radius1, Center2, Radius2, true);
			Gizmos.DrawLine(tuple2.First, tuple2.Second);
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