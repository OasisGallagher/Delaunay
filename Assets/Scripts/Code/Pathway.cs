using System.Collections.Generic;
using UnityEngine;

namespace Delaunay
{
	public class Pathway
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

			for (int i = 1; i < points.Length; ++i)
			{
				if(lengths[i] >= remaining)
				{
					ans = Vector3.Lerp(points[i - 1], points[i], remaining / lengths[i]);
					break;
				}

				remaining -= lengths[i];
			}

			return ans;
		}

		void Recalculate()
		{
			if (points == null)
			{
				lengths = null;
				normals = null;
				totalLength = 0f;
				return;
			}

			lengths = new float[points.Length];
			for (int i = 1; i < points.Length; ++i)
			{
				normals[i] = points[i] - points[i - 1];
				lengths[i] = normals[i].magnitude2();
				normals[i] /= lengths[i];
				totalLength += lengths[i];
			}
		}

		Vector3[] points = null;
		Vector3[] normals = null;

		float[] lengths = null;
		float totalLength = 0f;
	}
}
