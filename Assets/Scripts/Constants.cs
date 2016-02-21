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
		public const string kXmlVertex = "Vertex";
		public const string kXmlEdge = "Edge";
		public const string kXmlTriangle = "Triangle";

		public const string kXmlRoot = "Root";
		public const string kXmlAllVertices = "AllVertices";
		public const string kXmlAllEdges = "AllEdges";
		public const string kXmlAllTriangles = "AllTriangles";

		public const float kPanelWidth = 60;
		public const float kConvexHullGizmosHeight = 0.7f;
		public const float kNeighborTriangleGizmosHeight = 0.5f;

		public const int kMaxStackCapacity = 4096;
		public const int kDebugInvalidCycle = 32;

		public static readonly Vector3 kPathRendererOffset = new Vector3(0, 0.42f, 0);
		public static readonly Vector3 kTriangleGizmosOffset = new Vector3(0, 0.4f, 0);
		public static readonly Vector3 kTriangleMeshOffset = new Vector3(0, 0.1f, 0);
		public static readonly Vector3 kHalfEdgeGizmosOffset = new Vector3(0, 0.3f, 0);
		public static readonly Vector3 kEdgeGizmosOffset = new Vector3(0, 0.3f, 0);
		public static readonly int[] kTriangleIndices = new int[] { 0, 2, 1 };
		public static readonly Vector2[] kUV = new Vector2[] { Vector2.zero, Vector2.zero, Vector2.zero };

		public static readonly VertexComparer kVertexComparer = new VertexComparer();

		public static readonly Material kWalkableMaterial = (Material)Resources.Load("Materials/Walkable");
		public static readonly Material kBlockMaterial = (Material)Resources.Load("Materials/Block");
	}
}
