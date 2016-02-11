using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HalfEdgeContainer = System.Collections.Generic.SortedDictionary<Delaunay.Vertex, System.Collections.Generic.List<Delaunay.HalfEdge>>;

namespace Delaunay
{
	public static class GeomManager
	{
		static HalfEdgeContainer container = new HalfEdgeContainer(EditorConstants.kVertexComparer);

		public static void Add(HalfEdge edge)
		{
			List<HalfEdge> list = null;
			Utility.Verify(edge.Pair != null, "Invalid Edge, ID = " + edge.ID);

			if (!container.TryGetValue(edge.Src, out list))
			{
				container.Add(edge.Src, list = new List<HalfEdge>());
			}

			list.Add(edge);
		}

		public static void Add(Vertex vertex)
		{
			Utility.Verify(!container.ContainsKey(vertex));
			container.Add(vertex, new List<HalfEdge>());
		}

		public static void Remove(HalfEdge edge)
		{
			List<HalfEdge> list = container[edge.Src];
			Utility.Verify(list.Remove(edge));
			if (list.Count == 0)
			{
				container.Remove(edge.Src);
			}
		}

		public static void Clear()
		{
			AllTriangles.ForEach(facet =>
			{
				Triangle.Release(facet);
			});

			container.Clear();

			Vertex.ResetIDGenerator();
			HalfEdge.ResetIDGenerator();
			Triangle.ResetIDGenerator();
		}

		public static Vertex FindVertex(Vector3 position)
		{
			List<Vertex> vertices = AllVertices;

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

		public static List<HalfEdge> GetRays(Vertex vertex)
		{
			List<HalfEdge> answer = null;
			container.TryGetValue(vertex, out answer);
			return answer ?? new List<HalfEdge>();
		}

		public static List<Vertex> AllVertices
		{
			get { return new List<Vertex>(container.Keys); }
		}

		public static List<HalfEdge> AllEdges
		{
			get
			{
				List<HalfEdge> answer = new List<HalfEdge>();
				foreach (Vertex vertex in AllVertices)
				{
					List<HalfEdge> rays = GetRays(vertex);
					answer.AddRange(rays);
				}

				return answer;
			}
		}

		public static List<Triangle> AllTriangles
		{
			get
			{
				if (container.Count == 0) { return new List<Triangle>(); }
				return CollectTriangles();
			}
		}

		static List<Triangle> CollectTriangles()
		{
			Triangle face = null;
			foreach (List<HalfEdge> edgeContainer in container.Values)
			{
				foreach (HalfEdge edge in edgeContainer)
				{
					if ((face = edge.Face) != null) { break; }
				}

				if (face != null) { break; }
			}

			Utility.Verify(face != null);
			HashSet<int> visitedFaces = new HashSet<int> { face.ID };

			Stack<Triangle> stack = new Stack<Triangle>();
			stack.Push(face);

			List<Triangle> answer = new List<Triangle>();

			for (; stack.Count != 0; )
			{
				face = stack.Pop();
				answer.Add(face);

				foreach (HalfEdge edge in face.BoundingEdges)
				{
					if (edge.Pair.Face != null && !visitedFaces.Contains(edge.Pair.Face.ID))
					{
						visitedFaces.Add(edge.Pair.Face.ID);
						stack.Push(edge.Pair.Face);
					}
				}
			}

			return answer;
		}
	}
}