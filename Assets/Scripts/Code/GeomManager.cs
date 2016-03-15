using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HalfEdgeContainer = System.Collections.Generic.SortedDictionary<Delaunay.Vertex, System.Collections.Generic.List<Delaunay.HalfEdge>>;
using ObstacleContainer = System.Collections.Generic.List<Delaunay.Obstacle>;

namespace Delaunay
{
	public static class GeomManager
	{
		static HalfEdgeContainer halfEdgeContainer = new HalfEdgeContainer(EditorConstants.kVertexComparer);
		static ObstacleContainer obstacleContainer = new ObstacleContainer();
		static TiledMap tiledMap = new TiledMap(Vector3.zero, 1f, 128, 128);

		public static void AddEdge(HalfEdge edge)
		{
			List<HalfEdge> list = null;
			Utility.Verify(edge.Pair != null, "Invalid Edge, ID = " + edge.ID);

			if (!halfEdgeContainer.TryGetValue(edge.Src, out list))
			{
				halfEdgeContainer.Add(edge.Src, list = new List<HalfEdge>());
			}

			list.Add(edge);
		}

		public static void AddVertex(Vertex vertex)
		{
			Utility.Verify(!halfEdgeContainer.ContainsKey(vertex));
			halfEdgeContainer.Add(vertex, new List<HalfEdge>());
		}

		public static void AddObstacle(Obstacle obstacle)
		{
			obstacleContainer.Add(obstacle);
		}

		public static void RemoveObstacle(Obstacle obstacle)
		{
			obstacleContainer.Remove(obstacle);
		}

		public static void AddTriangle(Triangle triangle)
		{
			Vector3[] list = new Vector3[] { triangle.A.Position, triangle.B.Position, triangle.C.Position };
			Vector3 center = MathUtility.Centroid(list);
			Tile tile = tiledMap.GetTile(center);
			Utility.Verify(tile != null, "Invalid tile at position " + center);
			if (tile != null && MathUtility.PolygonContains(list, center))
			{
				tile.Face = triangle;
			}
		}

		public static void RemoveTriangle(Triangle triangle)
		{
			Vector3[] list = new Vector3[] { triangle.A.Position, triangle.B.Position, triangle.C.Position };
			Vector3 center = MathUtility.Centroid(list);
			Tile tile = tiledMap.GetTile(center);
			Utility.Verify(tile != null, "Invalid tile at position " + center);
			if (tile != null && tile.Face == triangle)
			{
				tile.Face = null;
			}
		}

		public static Tuple2<int, Triangle> FindVertexContainedTriangle(Vector3 position)
		{
			Tuple2<int, Triangle> answer = new Tuple2<int, Triangle>();

			Tile startTile = tiledMap.GetTile(position);
			Triangle face = startTile != null ? startTile.Face : null;

			face = face ?? GeomManager.AllTriangles[0];

			for (; face != null; )
			{
				if (face.HasVertex(position))
				{
					answer.Set(-1, face);
					return answer;
				}

				int iedge = face.GetPointDirection(position);
				if (iedge >= 0)
				{
					answer.Set(iedge, face);
					return answer;
				}

				face = face.GetEdgeByDirection(iedge).Pair.Face;
			}

			return answer;
		}

		public static Obstacle GetObstacle(int ID)
		{
			return obstacleContainer.Find(item => { return item.ID == ID; });
		}

		public static void RemoveEdge(HalfEdge edge)
		{
			List<HalfEdge> list = halfEdgeContainer[edge.Src];
			Utility.Verify(list.Remove(edge));
			if (list.Count == 0)
			{
				halfEdgeContainer.Remove(edge.Src);
			}
		}

		public static void Clear()
		{
			AllTriangles.ForEach(facet =>
			{
				Triangle.Release(facet);
			});

			halfEdgeContainer.Clear();
			obstacleContainer.Clear();

			Vertex.VertexIDGenerator.Current = 0;
			HalfEdge.HalfEdgeIDGenerator.Current = 0;
			Triangle.TriangleIDGenerator.Current = 0;
			Obstacle.ObstacleIDGenerator.Current = 0;
		}

		public static Vertex FindVertex(Vector3 position)
		{
			List<Vertex> vertices = AllVertices;

			int low = 0, high = vertices.Count - 1, mid = 0;
			for (; low <= high; )
			{
				mid = low + (high - low) / 2;
				int comp = position.compare2(vertices[mid].Position);

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
			halfEdgeContainer.TryGetValue(vertex, out answer);
			return answer ?? new List<HalfEdge>();
		}

		public static List<Vertex> AllVertices
		{
			get { return new List<Vertex>(halfEdgeContainer.Keys); }
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
				if (halfEdgeContainer.Count == 0) { return new List<Triangle>(); }
				return CollectTriangles();
			}
		}

		public static List<Obstacle> AllObstacles
		{
			get { return obstacleContainer; }
		}

		public static TiledMap Map
		{
			get { return tiledMap; }
		}

		static List<Triangle> CollectTriangles()
		{
			Triangle face = null;
			foreach (List<HalfEdge> edgeContainer in halfEdgeContainer.Values)
			{
				foreach (HalfEdge edge in edgeContainer)
				{
					if ((face = edge.Face) != null) { break; }
				}

				if (face != null) { break; }
			}

			List<Triangle> answer = new List<Triangle>();

			if (face == null) { return answer; }
 
			HashSet<int> visitedFaces = new HashSet<int> { face.ID };

			Queue<Triangle> queue = new Queue<Triangle>();
			queue.Enqueue(face);

			for (; queue.Count != 0; )
			{
				face = queue.Dequeue();
				answer.Add(face);

				foreach (HalfEdge edge in face.BoundingEdges)
				{
					if (edge.Pair.Face != null && !visitedFaces.Contains(edge.Pair.Face.ID))
					{
						visitedFaces.Add(edge.Pair.Face.ID);
						queue.Enqueue(edge.Pair.Face);
					}
				}
			}

			return answer;
		}
	}
}