using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Delaunay
{
	/// <summary>
	/// ����Ƚ���.
	/// </summary>
	public class VertexComparer : IComparer<Vertex>
	{
		public int Compare(Vertex lhs, Vertex rhs)
		{
			return lhs.Position.compare2(rhs.Position);
		}
	}

	/// <summary>
	/// �༭������.
	/// </summary>
	public static class EditorConstants
	{
		public const int kMaxStackCapacity = 4096;

		/// <summary>
		/// ���Ŀ¼.
		/// </summary>
		public static readonly string kOutputFolder = Path.Combine(Application.dataPath, "Output");

		public static readonly Vector3 kPathOffset = new Vector3(0, 0.3f, 0f);
		public static readonly Vector3 kMeshOffset = new Vector3(0, 0.1f, 0);

		/// <summary>
		/// ����Ƚ���.
		/// </summary>
		public static readonly VertexComparer kVertexComparer = new VertexComparer();
	}
}
