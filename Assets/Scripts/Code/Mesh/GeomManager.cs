using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Delaunay
{
	/// <summary>
	/// 点, 边, 三角形, 障碍物, 边集的管理器.
	/// </summary>
	public class GeomManager
	{
		/// <summary>
		/// 场景上的格子, 用来加速查找.
		/// </summary>
		TiledMap tiledMap = null;

		/// <summary>
		/// 障碍物列表.
		/// </summary>
		List<Obstacle> obstacleContainer = new List<Obstacle>();

		/// <summary>
		/// 边集列表.
		/// </summary>
		List<BorderSet> borderSetContainer = new List<BorderSet>();

		/// <summary>
		/// 每个顶点, 对应从它出发的所有边.
		/// </summary>
		SortedDictionary<Vertex, List<HalfEdge>> halfEdgeContainer = new SortedDictionary<Vertex, List<HalfEdge>>(EditorConstants.kVertexComparer);

		/// <summary>
		/// 初始化管理器, 地图起点为origin, 宽度为width, 高度为height.
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
		/// 创建一个位置在position的顶点.
		/// </summary>
		public Vertex CreateVertex(BinaryReader reader)
		{
			Vertex ans = new Vertex(Vector3.zero);
			ans.ReadBinary(reader);

			AddVertex(ans);

			return ans;
		}

		/// <summary>
		/// 创建从src到dest的边, 如果它已经存在, 直接返回它.
		/// </summary>
		public HalfEdge CreateEdge(Vertex src, Vertex dest)
		{
			HalfEdge self = GetRays(src).Find(item => { return item.Dest == dest; });

			// 如果该边不存在, 进行创建.
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
		/// 从reader中创建/查找边, 并初始化.
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
		/// 释放edge及edge.Pair.
		/// </summary>
		public void ReleaseEdge(HalfEdge edge)
		{
			ReleaseHalfEdge(edge.Pair);
			ReleaseHalfEdge(edge);
		}

		/// <summary>
		/// 创建一个空的triangle.
		/// </summary>
		public Triangle CreateTriangle()
		{
			return new Triangle();
		}

		/// <summary>
		/// 创建triangle, 并从reader中初始化.
		/// </summary>
		public Triangle CreateTriangle(BinaryReader reader, IDictionary<int, HalfEdge> container)
		{
			Triangle answer = CreateTriangle();

			answer.ReadBinary(reader, container);

			RasterizeTriangle(answer);

			return answer;
		}

		/// <summary>
		/// 创建边ab, bc, ca, 及三角形, 如果该三角形已经存在, 直接返回它.
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
		/// 释放triangle.
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
		/// 创建由boundingEdges包围的障碍物.
		/// </summary>
		public Obstacle CreateObstacle(List<HalfEdge> boundingEdges)
		{
			Obstacle answer = new Obstacle();
			answer.BoundingEdges = boundingEdges;
			obstacleContainer.Add(answer);
			return answer;
		}

		/// <summary>
		/// 从reader创建并初始化障碍物.
		/// </summary>
		public Obstacle CreateObstacle(BinaryReader reader, IDictionary<int, HalfEdge> container)
		{
			Obstacle answer = new Obstacle();
			answer.ReadBinary(reader, container);
			obstacleContainer.Add(answer);
			return answer;
		}

		/// <summary>
		/// 获取指定ID为障碍物.
		/// </summary>
		public Obstacle GetObstacle(int ID)
		{
			return obstacleContainer.Find(item => { return item.ID == ID; });
		}

		/// <summary>
		/// 释放障碍物.
		/// </summary>
		public void ReleaseObstacle(Obstacle obstacle)
		{
			obstacleContainer.Remove(obstacle);
		}

		/// <summary>
		/// 创建由boundingEdges包围的边集.
		/// </summary>
		public BorderSet CreateBorderSet(List<HalfEdge> boundingEdges)
		{
			BorderSet answer = new BorderSet();
			answer.BoundingEdges = boundingEdges;
			borderSetContainer.Add(answer);
			return answer;
		}

		/// <summary>
		/// 从reader创建并初始化边集.
		/// </summary>
		public BorderSet CreateBorderSet(BinaryReader reader, IDictionary<int, HalfEdge> container)
		{
			BorderSet answer = new BorderSet();
			answer.ReadBinary(reader, container);
			borderSetContainer.Add(answer);
			return answer;
		}

		/// <summary>
		/// 获取指定ID的边集.
		/// </summary>
		public BorderSet GetBorderSet(int ID)
		{
			return borderSetContainer.Find(item => { return item.ID == ID; });
		}

		/// <summary>
		/// 释放边集.
		/// </summary>
		public void ReleaseBorderSet(BorderSet borderSet)
		{
			borderSetContainer.Remove(borderSet);
		}

		/// <summary>
		/// 查找包含position的三角形.
		/// <para>返回:</para>
		/// <para>i&lt;0, face: position与face的第-(i+1)个顶点重合.</para>
		/// <para>i==0, face: position在face内.</para>
		/// <para>i&gt;0, face: position在face的第i条边上.</para>
		/// </summary>
		public Tuple2<int, Triangle> FindVertexContainedTriangle(Vector3 position)
		{
			Tuple2<int, Triangle> answer = new Tuple2<int, Triangle>();

			Tile startTile = tiledMap[position];
			
			// 查找开始的三角形, 如果未找到, 从任意(这里去第1个)三角形开始.
			Triangle face = startTile != null ? startTile.Face : null;

			face = face ?? AllTriangles[0];

			for (; face != null; )
			{
				// 顶点重合.
				int index = face.VertexIndex(position);
				if (index >= 0)
				{
					answer.Set(-(index + 1), face);
					return answer;
				}

				// 在边上或者在三角形内.
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
		/// 将triangle映射到TiledMap上.
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
		/// 将triangle的映射从TiledMap上去掉.
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
		/// 清空管理器.
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
		/// 查找位置在position的点.
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
		/// 查找从vertex出发的边.
		/// </summary>
		public List<HalfEdge> GetRays(Vertex vertex)
		{
			List<HalfEdge> answer = null;
			halfEdgeContainer.TryGetValue(vertex, out answer);
			return answer ?? new List<HalfEdge>();
		}

		/// <summary>
		/// 所有的点.
		/// </summary>
		public List<Vertex> AllVertices
		{
			get { return new List<Vertex>(halfEdgeContainer.Keys); }
		}

		/// <summary>
		/// 所有的边.
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
		/// 所有的三角形.
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
		/// 所有的障碍物.
		/// </summary>
		public List<Obstacle> AllObstacles
		{
			get { return obstacleContainer; }
		}

		/// <summary>
		/// 所有的边集.
		/// </summary>
		public List<BorderSet> AllBorderSets
		{
			get { return borderSetContainer; }
		}

		/// <summary>
		/// 格子地图.
		/// </summary>
		public TiledMap Map
		{
			get { return tiledMap; }
		}

		/// <summary>
		/// 向管理器加入一条反序列化后的边.
		/// <para>仅供反序列化时使用!</para>
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
		/// 查找与triangle的外接矩形相交的格子.
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
