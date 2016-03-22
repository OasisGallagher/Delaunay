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
		public const string kXmlObstacle = "Obstacle";

		public const string kXmlRoot = "Root";
		public const string kXmlAllVertices = "AllVertices";
		public const string kXmlAllEdges = "AllEdges";
		public const string kXmlAllTriangles = "AllTriangles";
		public const string kXmlAllObstacles = "AllObstacles";

		public const float kPanelWidth = 60;
		
		public const int kMaxStackCapacity = 4096;
		public const int kDebugInvalidCycle = 32;

		public static readonly string kMeshPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "delaunay.xml");

		public static readonly Vector3 kPathOffset = new Vector3(0, 0.7f, 0f);
		public static readonly Vector3 kMeshOffset = new Vector3(0, 0.1f, 0);
		public static readonly Vector3 kNewPolygonPreviewOffset = new Vector3(0, 0.8f, 0);

		public static readonly VertexComparer kVertexComparer = new VertexComparer();
	}
}
