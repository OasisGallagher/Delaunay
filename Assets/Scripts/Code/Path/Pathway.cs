using System.Collections.Generic;
using UnityEngine;

namespace Delaunay
{
	/// <summary>
	/// 路径.
	/// </summary>
	[RequireComponent(typeof(LineRenderer))]
	public class Pathway : MonoBehaviour
	{
		/// <summary>
		/// 路径点.
		/// </summary>
		public Vector3[] Points
		{
			get { return points; }
			set
			{
				points = value;
				Recalculate();
			}
		}

		/// <summary>
		/// 路径总长度.
		/// </summary>
		public float Length
		{
			get { return totalLength; }
		}

		/// <summary>
		/// 获取长度为length处的点坐标.
		/// </summary>
		public Vector3 DistanceToPoint(float distance)
		{
			Utility.Assert(distance > 0f);

			// 超出路径.
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
					// 插值.
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
				return;
			}

			lengths = new float[points.Length];

			for (int i = 1; i < points.Length; ++i)
			{
				Vector3 diff = points[i] - points[i - 1];
				lengths[i] = diff.magnitude2();
				totalLength += lengths[i];
			}

			pathRenderer.SetVertexCount(points.Length);
			for (int i = 0; i < points.Length; ++i)
			{
				pathRenderer.SetPosition(i, points[i] + EditorConstants.kPathOffset);
			}
		}

		/// <summary>
		/// 渲染路径.
		/// </summary>
		LineRenderer pathRenderer;

		/// <summary>
		/// 路径点.
		/// </summary>
		Vector3[] points = null;

		/// <summary>
		/// lengths[i]表示points[i-1]到points[i]的长度.
		/// </summary>
		float[] lengths = null;

		/// <summary>
		/// 路径总长度.
		/// </summary>
		float totalLength = 0f;
	}
}
