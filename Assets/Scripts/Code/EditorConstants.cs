using System;
using System.Collections.Generic;
using UnityEngine;

namespace Delaunay
{
	public class VertexComparer : IComparer<Vertex>
	{
		public int Compare(Vertex lhs, Vertex rhs)
		{
			return lhs.Position.compare2(rhs.Position);
		}
	}

	public static class EditorConstants
	{
		public const float kPanelWidth = 60;
		
		public const int kMaxStackCapacity = 4096;
		public const int kDebugInvalidCycle = 32;

		public static readonly Vector3 kPathOffset = new Vector3(0, 0.7f, 0f);
		public static readonly Vector3 kMeshOffset = new Vector3(0, 0.1f, 0);
		public static readonly Vector3 kNewPolygonPreviewOffset = new Vector3(0, 0.8f, 0);

		public static readonly VertexComparer kVertexComparer = new VertexComparer();
	}
}
