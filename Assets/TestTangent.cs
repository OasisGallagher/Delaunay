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
			MathUtility.DrawGizmosCircle(Center2, Radius2, Color.white);
			Gizmos.DrawLine(Center1, MathUtility.GetTangent(Center2, 2, Center1, true));
			/*
			MathUtility.DrawGizmosCircle(Center1, Radius1);
			MathUtility.DrawGizmosCircle(Center2, Radius2);
			Tuple2<Vector3, Vector3> tuple2 = MathUtility.GetOutterTangent(Center1, Radius1, Center2, Radius2, true);
			Gizmos.DrawLine(tuple2.First, tuple2.Second);
			 */
		}
	}
}