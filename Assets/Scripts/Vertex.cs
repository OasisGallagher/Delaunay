using System;
using UnityEngine;
using System.Collections.Generic;

namespace Delaunay
{
	public class Vertex
	{
		public int ID;

		/// <summary>
		/// One of the half-edges emanating from this vertex.
		/// </summary>
		public HalfEdge Edge;

		public Vector3 Position;

		public static Vertex Create(Vector3 position)
		{
			List<Vertex> vertices = HalfEdgeManager.SortedVertices;
			Vertex answer = new Vertex(position);
			int index = vertices.BinarySearch(answer, EditorConstants.kVertexComparer);
			return index >= 0 ? vertices[index] : answer;
		}

		Vertex(Vector3 position)
		{
			ID = vertexID++;
			this.Position = position;
		}

		public override string ToString()
		{
			return ID + "@" + Position;
		}

		public override bool Equals(object obj)
		{
			return obj is Vertex && Utility.Equals2D((obj as Vertex).Position, Position);
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		static int vertexID = 0;
	}
}
