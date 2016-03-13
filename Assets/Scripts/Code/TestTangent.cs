using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Delaunay
{
	public class TestTangent : MonoBehaviour
	{
		public Vector3 A = Vector3.zero;
		public Vector3 B = new Vector3(3f, 0, 0);
		public Vector3 C = new Vector3(0, 0, 2.3f);

		public Vector3 Ref = Vector3.zero;

		public float Radius = 0.5f;

		void OnDrawGizmos()
		{
			Gizmos.DrawLine(A, B);
			Gizmos.DrawLine(B, C);
			Gizmos.DrawLine(C, A);

			Vector3[] triangle = new Vector3[] { A, B, C };
			MathUtility.Shink(triangle, -0.7f);
			Gizmos.DrawLine(triangle[0], triangle[1]);
			Gizmos.DrawLine(triangle[1], triangle[2]);
			Gizmos.DrawLine(triangle[2], triangle[0]);
		}
	}
}