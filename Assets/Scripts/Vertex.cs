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

		public Vertex(Vector3 position)
		{
			ID = vertexID++;
			this.Position = position;
		}

		// Error-prone
		/*public List<HalfEdge> Rays
		{
			get
			{
				if (Edge == null) { return new List<HalfEdge>(); }
				return GetRays();
			}
		}*/

		public override string ToString()
		{
			return ID + "@" + Position;
		}

		/*List<HalfEdge> GetRays()
		{
			List<HalfEdge> answer = new List<HalfEdge> { Edge };
			for (HalfEdge current = Edge; (current = current.Pair.Next) != Edge && current != null; )
			{
				answer.Add(current);
				if (answer.Count >= EditorConstants.kDebugInvalidCycle)
				{
					Debug.LogError("Too may rays");
					break;
				}
			}

			return answer;
		}*/

		static int vertexID = 0;
	}
}
