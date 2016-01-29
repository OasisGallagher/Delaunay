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
			Utility.Verify(FindExistingVertex(position) == null, "Duplicate vertex at position " + position);
			Vertex ans = new Vertex(position);
			GeomManager.Add(ans);
			return ans;
		}

		static Vertex FindExistingVertex(Vector3 position)
		{
			List<Vertex> vertices = GeomManager.SortedVertices;

			int low = 0, high = vertices.Count - 1, mid = 0;
			for (; low <= high; )
			{
				mid = low + (high - low) / 2;
				int comp = Utility.CompareTo2D(position, vertices[mid].Position);

				if (comp == 0) { return vertices[mid]; }
				if (comp < 0)
				{
					high = mid - 1;
				}
				else
				{
					low = mid + 1;
				}
			}

			return null;
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
