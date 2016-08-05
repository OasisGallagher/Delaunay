using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Delaunay
{
	/// <summary>
	/// ��, ��, ������, �ϰ���, �߼��Ĺ�����.
	/// </summary>
	public class GeomManager
	{
		/// <summary>
		/// �����ϵĸ���, �������ٲ���.
		/// </summary>
		TiledMap tiledMap = null;

		/// <summary>
		/// �ϰ����б�.
		/// </summary>
		List<Obstacle> obstacleContainer = new List<Obstacle>();

		/// <summary>
		/// �߼��б�.
		/// </summary>
		List<BorderSet> borderSetContainer = new List<BorderSet>();

		/// <summary>
		/// ÿ������, ��Ӧ�������������б�.
		/// </summary>
		SortedDictionary<Vertex, List<HalfEdge>> halfEdgeContainer = new SortedDictionary<Vertex, List<HalfEdge>>(EditorConstants.kVertexComparer);

		/// <summary>
		/// ��ʼ��������, ��ͼ���Ϊorigin, ���Ϊwidth, �߶�Ϊheight.
		/// </summary>
		public GeomManager(Vector3 origin, float width, float height)
		{
			tiledMap = new TiledMap(origin, 1f, Mathf.CeilToInt(height), Mathf.CeilToInt(width));
		}

		public Vertex CreateVertex(Vector3 position)
		{
			Utility.Verify(FindVertex(position) == null, "Duplicate vertex at position " + position);
			Vertex ans = new Vertex(position);

			AddVertex(ans);

			return ans;
		}

		/// <summary>
		/// ����һ��λ����position�Ķ���.
		/// </summary>
		public Vertex CreateVertex(BinaryReader reader)
		{
			Vertex ans = new Vertex(Vector3.zero);
			ans.ReadBinary(reader);

			AddVertex(ans);

			return ans;
		}

		/// <summary>
		/// ������src��dest�ı�, ������Ѿ�����, ֱ�ӷ�����.
		/// </summary>
		public HalfEdge CreateEdge(Vertex src, Vertex dest)
		{
			HalfEdge self = GetRays(src).Find(item => { return item.Dest == dest; });

			// ����ñ߲�����, ���д���.
			if (self == null)
			{
				self = new HalfEdge();
				self.Dest = dest;

				HalfEdge other = new HalfEdge();
				other.Dest = src;

				self.Pair = other;
				other.Pair = self;

				/*src.Edge = self;
				dest.Edge = other;
				*/

				AddUnserializedEdge(self);
				AddUnserializedEdge(other);
			}

			return self;
		}

		/// <summary>
		/// ��reader�д���/���ұ�, ����ʼ��.
		/// </summary>
		public HalfEdge CreateEdge(BinaryReader reader, List<Vertex> vertices, IDictionary<int, HalfEdge> container)
		{
			HalfEdge answer = null;
			int edgeID = reader.ReadInt32();

			if (!container.TryGetValue(edgeID, out answer))
			{
				container.Add(edgeID, answer = new HalfEdge());
				answer.ID = edgeID;
			}

			answer.ReadBinary(reader, vertices, container);
			return answer;
		}

		/// <summary>
		/// �ͷ�edge��edge.Pair.
		/// </summary>
		public void ReleaseEdge(HalfEdge edge)
		{
			ReleaseHalfEdge(edge.Pair);
			ReleaseHalfEdge(edge);
		}

		/// <summary>
		/// ����һ���յ�triangle.
		/// </summary>
		public Triangle CreateTriangle()
		{
			return new Triangle();
		}

		/// <summary>
		/// ����triangle, ����reader�г�ʼ��.
		/// </summary>
		public Triangle CreateTriangle(BinaryReader reader, IDictionary<int, HalfEdge> container)
		{
			Triangle answer = CreateTriangle();

			answer.ReadBinary(reader, container);

			RasterizeTriangle(answer);

			return answer;
		}

		/// <summary>
		/// ������ab, bc, ca, ��������, ������������Ѿ�����, ֱ�ӷ�����.
		/// </summary>
		public Triangle CreateTriangle(Vertex a, Vertex b, Vertex c)
		{
			HalfEdge ab = CreateEdge(a, b);
			HalfEdge bc = CreateEdge(b, c);
			HalfEdge ca = CreateEdge(c, a);

			if (ab.Face != null)
			{
				Utility.Verify(ab.Face == bc.Face && bc.Face == ca.Face);
				return ab.Face;
			}

			ab.Next = bc;
			bc.Next = ca;
			ca.Next = ab;

			Triangle answer = CreateTriangle();

			ab.Face = bc.Face = ca.Face = answer;
			answer.Edge = ab;
			RasterizeTriangle(answer);

			return answer;
		}

		/// <summary>
		/// �ͷ�triangle.
		/// </summary>
		public void ReleaseTriangle(Triangle triangle)
		{
			UnrasterizeTriangle(triangle);

			foreach (HalfEdge edge in triangle.BoundingEdges)
			{
				if (edge.Face == triangle)
				{
					edge.Face = null;
					edge.Next = null;
				}

				if (edge.Face == null && edge.Pair.Face == null)
				{
					ReleaseEdge(edge);
				}
			}
		}

		/// <summary>
		/// ������boundingEdges��Χ���ϰ���.
		/// </summary>
		public Obstacle CreateObstacle(List<HalfEdge> boundingEdges)
		{
			Obstacle answer = new Obstacle();
			answer.BoundingEdges = boundingEdges;
			obstacleContainer.Add(answer);
			return answer;
		}

		/// <summary>
		/// ��reader��������ʼ���ϰ���.
		/// </summary>
		public Obstacle CreateObstacle(BinaryReader reader, IDictionary<int, HalfEdge> container)
		{
			Obstacle answer = new Obstacle();
			answer.ReadBinary(reader, container);
			obstacleContainer.Add(answer);
			return answer;
		}

		/// <summary>
		/// ��ȡָ��IDΪ�ϰ���.
		/// </summary>
		public Obstacle GetObstacle(int ID)
		{
			return obstacleContainer.Find(item => { return item.ID == ID; });
		}

		/// <summary>
		/// �ͷ��ϰ���.
		/// </summary>
		public void ReleaseObstacle(Obstacle obstacle)
		{
			obstacleContainer.Remove(obstacle);
		}

		/// <summary>
		/// ������boundingEdges��Χ�ı߼�.
		/// </summary>
		public BorderSet CreateBorderSet(List<HalfEdge> boundingEdges)
		{
			BorderSet answer = new BorderSet();
			answer.BoundingEdges = boundingEdges;
			borderSetContainer.Add(answer);
			return answer;
		}

		/// <summary>
		/// ��reader��������ʼ���߼�.
		/// </summary>
		public BorderSet CreateBorderSet(BinaryReader reader, IDictionary<int, HalfEdge> container)
		{
			BorderSet answer = new BorderSet();
			answer.ReadBinary(reader, container);
			borderSetContainer.Add(answer);
			return answer;
		}

		/// <summary>
		/// ��ȡָ��ID�ı߼�.
		/// </summary>
		public BorderSet GetBorderSet(int ID)
		{
			return borderSetContainer.Find(item => { return item.ID == ID; });
		}

		/// <summary>
		/// �ͷű߼�.
		/// </summary>
		public void ReleaseBorderSet(BorderSet borderSet)
		{
			borderSetContainer.Remove(borderSet);
		}

		/// <summary>
		/// ���Ұ���position��������.
		/// <para>����:</para>
		/// <para>i&lt;0, face: position��face�ĵ�-(i+1)�������غ�.</para>
		/// <para>i==0, face: position��face��.</para>
		/// <para>i&gt;0, face: position��face�ĵ�i������.</para>
		/// </summary>
		public Tuple2<int, Triangle> FindVertexContainedTriangle(Vector3 position)
		{
			Tuple2<int, Triangle> answer = new Tuple2<int, Triangle>();

			Tile startTile = tiledMap[position];
			
			// ���ҿ�ʼ��������, ���δ�ҵ�, ������(����ȥ��1��)�����ο�ʼ.
			Triangle face = startTile != null ? startTile.Face : null;

			face = face ?? AllTriangles[0];

			for (; face != null; )
			{
				// �����غ�.
				int index = face.VertexIndex(position);
				if (index >= 0)
				{
					answer.Set(-(index + 1), face);
					return answer;
				}

				// �ڱ��ϻ�������������.
				int iedge = face.GetPointDirection(position);
				if (iedge >= 0)
				{
					answer.Set(iedge, face);
					return answer;
				}

				face = face.GetEdgeByIndex(iedge).Pair.Face;
			}

			return answer;
		}

		/// <summary>
		/// ��triangleӳ�䵽TiledMap��.
		/// </summary>
		public void RasterizeTriangle(Triangle triangle)
		{
			Vector3[] list = new Vector3[] { triangle.A.Position, triangle.B.Position, triangle.C.Position };
			TiledMapRegion tmr = FindTriangleBoundingRectOverlappedTiles(triangle);

			for (; tmr.MoveNext(); )
			{
				Tuple2<int, int> current = tmr.Current;
				Tile tile = tiledMap[current.First, current.Second];
				Vector3 center = tiledMap.GetTileCenter(current.First, current.Second);
				if (MathUtility.PolygonContains(list, center))
				{
					tile.Face = triangle;
				}
			}
		}

		/// <summary>
		/// ��triangle��ӳ���TiledMap��ȥ��.
		/// </summary>
		public void UnrasterizeTriangle(Triangle triangle)
		{
			TiledMapRegion tmr = FindTriangleBoundingRectOverlappedTiles(triangle);
			for (; tmr.MoveNext(); )
			{
				Tile tile = tiledMap[tmr.Current.First, tmr.Current.Second];
				if (tile.Face == triangle)
				{
					tile.Face = null;
				}
			}
		}

		/// <summary>
		/// ��չ�����.
		/// </summary>
		public void Clear()
		{
			AllTriangles.ForEach(triangle =>
			{
				ReleaseTriangle(triangle);
			});

			halfEdgeContainer.Clear();
			obstacleContainer.Clear();

			Vertex.VertexIDGenerator.Current = 0;
			HalfEdge.HalfEdgeIDGenerator.Current = 0;
			Triangle.TriangleIDGenerator.Current = 0;
			Obstacle.ObstacleIDGenerator.Current = 0;
		}

		/// <summary>
		/// ����λ����position�ĵ�.
		/// </summary>
		public Vertex FindVertex(Vector3 position)
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

		/// <summary>
		/// ���Ҵ�vertex�����ı�.
		/// </summary>
		public List<HalfEdge> GetRays(Vertex vertex)
		{
			List<HalfEdge> answer = null;
			halfEdgeContainer.TryGetValue(vertex, out answer);
			return answer ?? new List<HalfEdge>();
		}

		/// <summary>
		/// ���еĵ�.
		/// </summary>
		public List<Vertex> AllVertices
		{
			get { return new List<Vertex>(halfEdgeContainer.Keys); }
		}

		/// <summary>
		/// ���еı�.
		/// </summary>
		public List<HalfEdge> AllEdges
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

		/// <summary>
		/// ���е�������.
		/// </summary>
		public List<Triangle> AllTriangles
		{
			get
			{
				if (halfEdgeContainer.Count == 0) { return new List<Triangle>(); }
				return CollectTriangles();
			}
		}

		/// <summary>
		/// ���е��ϰ���.
		/// </summary>
		public List<Obstacle> AllObstacles
		{
			get { return obstacleContainer; }
		}

		/// <summary>
		/// ���еı߼�.
		/// </summary>
		public List<BorderSet> AllBorderSets
		{
			get { return borderSetContainer; }
		}

		/// <summary>
		/// ���ӵ�ͼ.
		/// </summary>
		public TiledMap Map
		{
			get { return tiledMap; }
		}

		/// <summary>
		/// �����������һ�������л���ı�.
		/// <para>���������л�ʱʹ��!</para>
		/// </summary>
		public void AddUnserializedEdge(HalfEdge edge)
		{
			AddEdge(edge);
		}

		void AddVertex(Vertex vertex)
		{
			Utility.Verify(!halfEdgeContainer.ContainsKey(vertex));
			halfEdgeContainer.Add(vertex, new List<HalfEdge>());
		}

		void AddEdge(HalfEdge edge)
		{
			List<HalfEdge> list = null;
			Utility.Verify(edge.Pair != null, "Invalid Edge, ID = " + edge.ID);

			if (!halfEdgeContainer.TryGetValue(edge.Src, out list))
			{
				halfEdgeContainer.Add(edge.Src, list = new List<HalfEdge>());
			}

			list.Add(edge);
		}

		void ReleaseHalfEdge(HalfEdge edge)
		{
			List<HalfEdge> list = halfEdgeContainer[edge.Src];
			Utility.Verify(list.Remove(edge));
			if (list.Count == 0)
			{
				halfEdgeContainer.Remove(edge.Src);
			}
		}

		List<Triangle> CollectTriangles()
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

		/// <summary>
		/// ������triangle����Ӿ����ཻ�ĸ���.
		/// </summary>
		TiledMapRegion FindTriangleBoundingRectOverlappedTiles(Triangle triangle)
		{
			float xMin = Mathf.Min(triangle.A.Position.x, triangle.B.Position.x, triangle.C.Position.x);
			float xMax = Mathf.Max(triangle.A.Position.x, triangle.B.Position.x, triangle.C.Position.x);
			float zMin = Mathf.Min(triangle.A.Position.z, triangle.B.Position.z, triangle.C.Position.z);
			float zMax = Mathf.Max(triangle.A.Position.z, triangle.B.Position.z, triangle.C.Position.z);

			return tiledMap.GetTiles(xMin, xMax, zMin, zMax);
		}
	}
}
