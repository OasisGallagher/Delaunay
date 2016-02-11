using System.Collections;
using System.Collections.Generic;
using HalfEdgeContainer = System.Collections.Generic.SortedDictionary<Delaunay.Vertex, System.Collections.Generic.List<Delaunay.HalfEdge>>;

namespace Delaunay
{
	public static class GeomManager
	{
		static HalfEdgeContainer container = new HalfEdgeContainer(EditorConstants.kVertexComparer);

		public static void Add(HalfEdge edge)
		{
			List<HalfEdge> list = null;
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
		}

		public static List<HalfEdge> GetRays(Vertex vertex)
		{
			List<HalfEdge> answer = null;
			container.TryGetValue(vertex, out answer);
			return answer ?? new List<HalfEdge>();
		}

		public static List<Vertex> SortedVertices
		{
			get { return new List<Vertex>(container.Keys); }
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

				foreach (HalfEdge edge in face.AllEdges)
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