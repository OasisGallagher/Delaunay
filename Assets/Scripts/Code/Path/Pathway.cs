using System.Collections.Generic;
using UnityEngine;

namespace Delaunay
{
	[RequireComponent(typeof(LineRenderer))]
	public class Pathway : MonoBehaviour
	{
		public Vector3[] Points
		{
			get { return points; }
			set
			{
				points = value;
				Recalculate();
			}
		}

		public float Length
		{
			get { return totalLength; }
		}

		public Vector3 DistanceToPoint(float distance)
		{
			Utility.Assert(distance > 0f);
			if (distance >= totalLength)
			{
				return points.back();
			}

			float remaining = distance;
			Vector3 ans = Vector3.zero;

			int i = 1;
			for (; i < points.Length; ++i)
			{
				if (lengths[i] >= remaining)
				{
					ans = Vector3.Lerp(points[i - 1], points[i], remaining / lengths[i]);
					break;
				}

				remaining -= lengths[i];
			}

			if (i >= points.Length)
			{
			}

			return ans;
		}

		void Start()
		{
			pathRenderer = GetComponent<LineRenderer>();
			pathRenderer.SetVertexCount(0);
		}

		void Recalculate()
		{
			totalLength = 0f;

			if (points == null)
			{
				lengths = null;
				normals = null;
				return;
			}

			lengths = new float[points.Length];
			normals = new Vector3[points.Length];

			for (int i = 1; i < points.Length; ++i)
			{
				normals[i] = points[i] - points[i - 1];
				lengths[i] = normals[i].magnitude2();
				normals[i] /= lengths[i];
				totalLength += lengths[i];
			}

			pathRenderer.SetVertexCount(points.Length);
			for (int i = 0; i < points.Length; ++i)
			{
				pathRenderer.SetPosition(i, points[i] + EditorConstants.kPathOffset);
			}
		}

		LineRenderer pathRenderer;

		Vector3[] points = null;
		Vector3[] normals = null;

		float[] lengths = null;
		float totalLength = 0f;
	}
}
