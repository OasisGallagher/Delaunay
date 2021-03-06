﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Delaunay
{
	public class DelaunayMesh : IPathTerrain
	{
		/// <summary>
		/// raycast的步进值.
		/// </summary>
		const float kRaycastSearchStep = 0.1f;
		
		/// <summary>
		/// 寻找有效点时的最大半径.
		/// </summary>
		const float kGetNearestMaxDistance = 8;

		/// <summary>
		/// 寻找有效点时的弧度步进值.
		/// </summary>
		const int kGetNearestRadianSampleCount = 8;

		/// <summary>
		/// 寻找有效点时的半径进值.
		/// </summary>
		const int kGetNearestDistanceSampleCount = 10;

		/// <summary>
		/// 边框顶点, 表示地图的边缘.
		/// </summary>
		List<Vector3> superBorder;
		
		/// <summary>
		/// 管理器. 
		/// </summary>
		GeomManager geomManager;

		/// <summary>
		/// 初始化网格, 起点为origin, 宽度为width, 高度为height.
		/// </summary>
		public DelaunayMesh(Vector3 origin, float width, float height)
		{
			Width = width;
			Height = height;
			Origin = origin;

			geomManager = new GeomManager(origin, width, height);
			superBorder = new List<Vector3>();
		}

		/// <summary>
		/// 加入边框(边框唯一).
		/// </summary>
		public void AddSuperBorder(IEnumerable<Vector3> vertices)
		{
			Utility.Verify(!HasSuperBorder);
			superBorder.AddRange(vertices);
			CreateSuperBorder(superBorder);
		}

		/// <summary>
		/// 加入障碍物.
		/// </summary>
		public Obstacle AddObstacle(IEnumerable<Vector3> vertices)
		{
			List<HalfEdge> polygonBoundingEdges = AddShape(vertices, true);
			Obstacle obstacle = geomManager.CreateObstacle(polygonBoundingEdges);
			obstacle.Mesh.ForEach(triangle => { triangle.Walkable = false; });
			return obstacle;
		}

		/// <summary>
		/// 移除ID为obstacleID的障碍物.
		/// </summary>
		public void RemoveObstacle(int obstacleID)
		{
			Obstacle obstacle = geomManager.GetObstacle(obstacleID);
			RemoveShape(obstacle.BoundingEdges);
			geomManager.ReleaseObstacle(obstacle);
		}

		/// <summary>
		/// 加入边集, close表示是否自动闭合.
		/// </summary>
		public BorderSet AddBorderSet(IEnumerable<Vector3> vertices, bool close)
		{
			List<HalfEdge> polygonBoundingEdges = AddShape(vertices, close);
			BorderSet borderSet = geomManager.CreateBorderSet(polygonBoundingEdges);
			return borderSet;
		}

		/// <summary>
		/// 移除ID为obstacleID的边集.
		/// </summary>
		public void RemoveBorderSet(int borderSetID)
		{
			BorderSet borderSet = geomManager.GetBorderSet(borderSetID);
			RemoveShape(borderSet.BoundingEdges);
			geomManager.ReleaseBorderSet(borderSet);
		}

		/// <summary>
		/// 清除网格, 不清理边框.
		/// </summary>
		public void ClearMesh()
		{
			geomManager.Clear();
			CreateSuperBorder(superBorder);
		}

		/// <summary>
		/// 清除网格和边框.
		/// </summary>
		public void ClearAll()
		{
			geomManager.Clear();
			superBorder.Clear();
		}

		/// <summary>
		/// 加载网格.
		/// </summary>
		public void Load(string path)
		{
			geomManager.Clear();
			MeshSerializer.Load(path, geomManager, superBorder);
		}

		/// <summary>
		/// 保存网格.
		/// </summary>
		public void Save(string path)
		{
			MeshSerializer.Save(path, geomManager, superBorder);
		}

		/// <summary>
		/// 计算半径为radius的物体从start移动到dest的路径.
		/// </summary>
		public List<Vector3> FindPath(Vector3 start, Vector3 dest, float radius)
		{
			Tuple2<int, Triangle> findResult = geomManager.FindVertexContainedTriangle(start);
			if (findResult.Second != null && !findResult.Second.Walkable)
			{
				Debug.LogError("Invalid start position " + start);
				return null;
			}

			Utility.Verify(findResult.Second != null, "Invalid start point");
			Triangle triangle1 = findResult.Second;

			findResult = geomManager.FindVertexContainedTriangle(dest);
			if (findResult.Second != null && !findResult.Second.Walkable)
			{
				Debug.LogError("Invalid dest position " + dest);
				return null;
			}

			Utility.Verify(findResult.Second != null, "Invalid dest point");

			return Pathfinding.FindPath(triangle1, start, findResult.Second, dest, radius);
		}

		/// <summary>
		/// 查找离position最近的, 可以容纳半径为radius的物体的有效点.
		/// </summary>
		public Vector3 GetNearestPoint(Vector3 position, float radius)
		{
			if (!IsValidPosition(position, radius) && !SearchValidPosition(ref position, radius, kGetNearestMaxDistance, kGetNearestDistanceSampleCount, kGetNearestRadianSampleCount))
			{
				Debug.LogError("Failed to GetNearestPoint for " + position);
			}

			position.y = GetTerrainHeight(position);
			return position;
		}

		/// <summary>
		/// 获取将半径为radius的物体, 从from移动到to, 可达的最远位置.
		/// </summary>
		public Vector3 Raycast(Vector3 from, Vector3 to, float radius)
		{
			// 当前正处在无效的位置.
			if (!IsValidPosition(from, radius))
			{
				Debug.Log("Stuck at " + from);
				return from;
			}

			return RaycastFrom(from, to, radius);
		}

		/// <summary>
		/// 半径为radius的物体, 在position是否有效.
		/// </summary>
		public bool IsValidPosition(Vector3 position, float radius)
		{
			Tuple2<int, Triangle> containedInfo = geomManager.FindVertexContainedTriangle(position);

			// 是否在网格上.
			if (containedInfo.Second == null || !containedInfo.Second.Walkable)
			{
				return false;
			}

			return IsValidMeshPosition(containedInfo, position, radius);
		}

		/// <summary>
		/// 获取position位置的场景高度.
		/// </summary>
		public float GetTerrainHeight(Vector3 position)
		{
			return position.y;
			/*
			Triangle triangle = geomManager.FindVertexContainedTriangle(position).Second;
			return MathUtility.LineCrossPlane(triangle.A.Position, triangle.B.Position,triangle.C.Position, position, Vector3.down).y;
			 */
		}

		/// <summary>
		/// 场景宽度.
		/// </summary>
		public float Width { get; private set; }

		/// <summary>
		/// 场景高度.
		/// </summary>
		public float Height { get; private set; }

		/// <summary>
		/// 场景原点.
		/// </summary>
		public Vector3 Origin { get; private set; }

		/// <summary>
		/// 是否已存在边框.
		/// </summary>
		public bool HasSuperBorder
		{
			get { return superBorder.Count > 0; }
		}

		/// <summary>
		/// 组成边框的点.
		/// </summary>
		public IEnumerable<Vector3> BorderVertices
		{
			get { return superBorder; }
		}

		/// <summary>
		/// 所以的点.
		/// </summary>
		public List<Vertex> AllVertices
		{
			get { return geomManager.AllVertices; }
		}

		/// <summary>
		/// 所有的边.
		/// </summary>
		public List<HalfEdge> AllEdges
		{
			get { return geomManager.AllEdges; }
		}

		/// <summary>
		/// 所有的三角形.
		/// </summary>
		public List<Triangle> AllTriangles
		{
			get { return geomManager.AllTriangles; }
		}

		/// <summary>
		/// 所有的障碍物.
		/// </summary>
		public List<Obstacle> AllObstacles
		{
			get { return geomManager.AllObstacles; }
		}

		/// <summary>
		/// 格子地图.
		/// </summary>
		public TiledMap Map
		{
			get { return geomManager.Map; }
		}

		/// <summary>
		/// 加入一个多边形, close表示是否自动闭合.
		/// </summary>
		List<HalfEdge> AddShape(IEnumerable<Vector3> container, bool close)
		{
			IEnumerator<Vector3> e = container.GetEnumerator();
			List<HalfEdge> polygonBoundingEdges = new List<HalfEdge>();

			// 空的container.
			if (!e.MoveNext()) { return polygonBoundingEdges; }

			Vertex prevVertex = geomManager.CreateVertex(e.Current);
			Vertex firstVertex = prevVertex;

			// 创建边和节点, 并收集包围边.
			for (; e.MoveNext(); )
			{
				Vertex currentVertex = geomManager.CreateVertex(e.Current);
				polygonBoundingEdges.AddRange(AddConstrainedEdge(prevVertex, currentVertex));
				prevVertex = currentVertex;
			}

			// 自动闭合.
			if (close)
			{
				polygonBoundingEdges.AddRange(AddConstrainedEdge(prevVertex, firstVertex));
			}

			return polygonBoundingEdges;
		}

		/// <summary>
		/// 加入一条从src到dest的约束边.
		/// <para>返回构造的约束边.</para>
		/// </summary>
		List<HalfEdge> AddConstrainedEdge(Vertex src, Vertex dest)
		{
			// 加入顶点.
			Append(src);
			Append(dest);

			List<HalfEdge> answer = new List<HalfEdge>();

			// 加入并收集约束边.
			// 在加入约束边src->dest的过程中, 可能遇到一个点v, v在src->dest上.
			// 因此, 约束边变为src->v, v->dest.
			// 此时, src替换为v, 继续迭代, 直到到达dest.
			for (; src != dest; )
			{
				answer.Add(AddConstrainedEdgeAt(ref src, dest));
			}

			return answer;
		}

		/// <summary>
		/// 添加src到dest的约束边.
		/// <para>如果之间遇到顶点v在该边上, 构造边src->v, 并更新src为v.</para>
		/// <para>如果之间没有其他顶点, 那么构造边src->dest, 更新src为dest.</para>
		/// <para>返回构造的约束边.</para>
		/// </summary>
		HalfEdge AddConstrainedEdgeAt(ref Vertex src, Vertex dest)
		{
			// 寻找以src为起点的, 且与src->dest相交的边.
			Tuple2<HalfEdge, CrossState> crossResult = new Tuple2<HalfEdge, CrossState>(null, CrossState.Parallel);
			foreach (HalfEdge ray in geomManager.GetRays(src))
			{
				if (FindCrossedEdge(out crossResult, ray, src.Position, dest.Position))
				{
					break;
				}
			}

			Utility.Verify(crossResult.Second != CrossState.Parallel);

			// 找到的边与src->dest完全重合.
			if (crossResult.Second == CrossState.FullyOverlaps)
			{
				crossResult.First.Constrained = true;
				src = crossResult.First.Dest;
				return crossResult.First;
			}

			Utility.Verify(crossResult.Second == CrossState.CrossOnSegment);

			// 处理相交边.
			return OnConstrainedEdgeCrossEdges(ref src, dest, crossResult.First);
		}

		/// <summary>
		/// 向网格内加入点.
		/// </summary>
		bool Append(Vertex v)
		{
			// 查找包含点v的位置的三角形.
			Tuple2<int, Triangle> answer = geomManager.FindVertexContainedTriangle(v.Position);

			// 与顶点重合, 表示该点已经存在.
			if (answer.First < 0) { return false; }

			// 点在三角形内.
			if (answer.First == 0)
			{
				InsertToTriangle(v, answer.Second);
			}
			// 点在边上.
			else
			{
				HalfEdge hitEdge = answer.Second.GetEdgeByIndex(answer.First);
				InsertOnEdge(v, answer.Second, hitEdge);
			}

			return true;
		}

		/// <summary>
		/// 获取将半径为radius的物体, 从from移动到to, 可达的最远位置.
		/// </summary>
		Vector3 RaycastFrom(Vector3 from, Vector3 to, float radius)
		{
			// 查找包含from的三角形. 
			Tuple2<int, Triangle> containedInfo = geomManager.FindVertexContainedTriangle(from);

			Utility.Verify(containedInfo.Second != null, "can not locate position " + from);

			// 点重合.
			if (containedInfo.First < 0)
			{
				Debug.LogError("Unhandled case. radius = " + radius);
				return from;
			}

			Vector3 border = Vector3.zero;
			// 点在三角形内, 那么检查三角形的包围边.
			if (containedInfo.First == 0)
			{
				border = RaycastWithEdges(containedInfo.Second.BoundingEdges, from, to, radius);
			}
			// 点在边上, 那么从该边开始检查.
			else
			{
				HalfEdge edge = containedInfo.Second.GetEdgeByIndex(containedInfo.First);
				border = RaycastFromEdge(edge, from, to, radius);
			}
			
			// border为最远可达的位置, 且该位置不一定有效. 反向, 从border开始查找.
			return SearchRaycastPosition(border, from, radius, kRaycastSearchStep);
		}

		/// <summary>
		/// from在edge上, 查找半径为radius的物体从from到to可达的最远位置.
		/// </summary>
		Vector3 RaycastFromEdge(HalfEdge edge, Vector3 from, Vector3 to, float radius)
		{
			// 起点==终点, 或者edge为约束边.
			if (from.equals2(to) || edge.Constrained || edge.Pair.Constrained)
			{
				return from;
			}

			// from位置无效.
			if (!IsValidPosition(from, radius))
			{
				return from;
			}

			// 根据from->to的方向, 确定下一个要访问的三角形是edge.Face还是edge.Pair.Face.
			if ((to - from).cross2(edge.Dest.Position - edge.Src.Position) > 0)
			{
				edge = edge.Pair;
			}

			// 不存在另一边的三角形.
			if (edge.Face == null)
			{
				return from;
			}

			// 检查另外两条边.
			return RaycastWithEdges(new HalfEdge[] { edge.Next, edge.Next.Next }, from, to, radius);
		}

		/// <summary>
		/// 从edges开始检查, 获取将半径为radius的物体, 从from移动到to, 可达的最远位置. 
		/// </summary>
		Vector3 RaycastWithEdges(IEnumerable<HalfEdge> edges, Vector3 from, Vector3 to, float radius)
		{
			if (from.equals2(to))
			{
				return to;
			}

			Vector2 segCrossAnswer = Vector2.zero;
			foreach (HalfEdge edge in edges)
			{
				// 检查相交.
				CrossState crossState = MathUtility.SegmentCross(out segCrossAnswer, from, to, edge.Src.Position, edge.Dest.Position);

				Utility.Verify(crossState == CrossState.CrossOnSegment || crossState == CrossState.CrossOnExtLine);

				// 交点不在线段上.
				if (segCrossAnswer.x < 0 || segCrossAnswer.x > 1) { continue; }

				// 如果交点在线段上, 那么表示该交点为当前最远位置. 再继续从这条边开始检查.
				if (crossState == CrossState.CrossOnSegment)
				{
					Vector3 cross = from + segCrossAnswer.x * (to - from);
					to = RaycastFromEdge(edge, cross, to, radius);
					break;
				}
			}

			return to;
		}

		/// <summary>
		/// 从from开始, 向to的方向, 每步前进step距离, 查找半径为radius的物体可达的最远位置.
		/// </summary>
		Vector3 SearchRaycastPosition(Vector3 from, Vector3 to, float radius, float step)
		{
			Vector3 dir = to - from;
			float dist = dir.magnitude2();
			dir.Normalize();

			int count = Mathf.FloorToInt(dist / step);
			for (int i = 0; i < count; ++i)
			{
				Vector3 position = i * step * dir + from;
				if (IsValidPosition(position, radius))
				{
					return position;
				}
			}

			return from;
		}

		/// <summary>
		/// 从position开始, 半径为maxDist的圆内, 查找有效的位置.
		/// </summary>
		bool SearchValidPosition(ref Vector3 position, float radius, float maxDist, int distSampleCount, int circularSampleCount)
		{
			float radianStep = Mathf.PI * 2f / circularSampleCount;
			float distStep = maxDist / distSampleCount;

			for (int i = 1; i <= distSampleCount; ++i)
			{
				Vector3 sample = position + Vector3.forward * i * distStep;
				for (int j = 0; j < circularSampleCount; ++j)
				{	
					Vector3 answer = MathUtility.Rotate(sample, j * radianStep, position);
					if (IsValidPosition(answer, radius))
					{
						position = answer;
						return true;
					}
				}
			}

			return false;
		}

		/// <summary>
		/// 半径为radius的物体在position位置, 是否有效.
		/// <para>containedInfo表示position所处的三角形信息.</para>
		/// </summary>
		bool IsValidMeshPosition(Tuple2<int, Triangle> containedInfo, Vector3 position, float radius)
		{
			List<HalfEdge> edges = new List<HalfEdge>();

			// 与顶点重合, 必然是无效的位置.
			if (containedInfo.First < 0) { return false; }

			// 在三角形内, 检查它的边.
			if (containedInfo.First == 0)
			{
				edges.Add(containedInfo.Second.AB);
				edges.Add(containedInfo.Second.BC);
				edges.Add(containedInfo.Second.CA);
			}
			// 在边上, 检查另外两条边.
			else
			{
				edges.Add(containedInfo.Second.GetEdgeByIndex(containedInfo.First));
				edges.Add(edges.back().Pair);
			}

			for (; edges.Count != 0; )
			{
				HalfEdge current = edges.popBack();

				// 圆心为position, 半径为radius的圆, 是否包含这条边的端点.
				if (MathUtility.PointInCircle(current.Src.Position, position, radius)
					|| MathUtility.PointInCircle(current.Dest.Position, position, radius))
				{
					return false;
				}

				// 该圆是否与这条边相交.
				if (MathUtility.MinDistance2Segment(position, current.Src.Position, current.Dest.Position) >= radius)
				{
					continue;
				}

				// 如果圆与边相交, 检查是不是该边是否为约束边.
				if (current.Constrained || current.Pair.Constrained)
				{
					return false;
				}

				if (current.Pair.Face == null) { continue; }

				// 继续检查边的另一侧三角形的另外两条边.
				edges.Add(current.Pair.Next);
				edges.Add(current.Pair.Next.Next);
			}

			return true;
		}

		/// <summary>
		/// 寻找极角排序的标尺, 即相对的0度角方向.
		/// </summary>
		Vertex FindBenchmark(List<Vertex> vertices, List<Triangle> triangles)
		{
			BitArray bits = new BitArray(vertices.Count);
			for (int i = 0; i < vertices.Count; ++i)
			{
				foreach (HalfEdge e in geomManager.GetRays(vertices[i]))
				{
					int index = vertices.IndexOf(e.Dest);
					if (index < 0) { continue; }
					if (triangles.Contains(e.Face)) { bits.Set(index, true); }
				}
			}

			for (int i = 0; i < bits.Count; ++i)
			{
				if (!bits[i])
				{
					return vertices[i];
				}
			}

			Utility.Verify(false, "Failed to find benchmark");
			return null;
		}

		/// <summary>
		/// 构造src到dest的约束边. crossedEdge为第一条相交边.
		/// </summary>
		HalfEdge OnConstrainedEdgeCrossEdges(ref Vertex src, Vertex dest, HalfEdge crossedEdge)
		{
			List<Vertex> upper = new List<Vertex>();
			List<Vertex> lower = new List<Vertex>();
			List<HalfEdge> edges = new List<HalfEdge>();

			// 收集相交的非约束边.
			Vertex newSrc = CollectCrossedUnconstrainedEdges(edges, lower, upper, src, dest, crossedEdge);

			// 清理非约束边两侧的三角形.
			for (int i = 0; i < edges.Count; ++i)
			{
				HalfEdge edge = edges[i];
				if (edge.Face != null)
				{
					geomManager.ReleaseTriangle(edge.Face);
				}

				if (edge.Pair.Face != null)
				{
					geomManager.ReleaseTriangle(edge.Pair.Face);
				}
			}

			// 对清理过后的区域三角形化(该区域不一定是多边形).
			CreateTriangles(PolygonTriangulation.Triangulate(lower, dest, src));
			CreateTriangles(PolygonTriangulation.Triangulate(upper, src, dest));

			// 标记约束边.
			HalfEdge constrainedEdge = geomManager.GetRays(src).Find(edge => { return edge.Dest == dest; });
			Utility.Verify(constrainedEdge != null);
			constrainedEdge.Constrained = true;

			src = newSrc;
			return constrainedEdge;
		}

		/// <summary>
		/// 收集与src到dest相交的约束边.
		/// </summary>
		Vertex CollectCrossedUnconstrainedEdges(List<HalfEdge> answer, List<Vertex> lowerVertices, List<Vertex> upperVertices, Vertex src, Vertex dest, HalfEdge start)
		{
			// src->dest向量.
			Vector3 srcDest = dest.Position - src.Position;

			Vertex current = src;

			// 遍历到dest所在的三角形为止.
			for (; !start.Face.Contains(dest.Position); )
			{
				Utility.Verify(!start.Constrained, "Crossed constrained edge");
				// 收集相交的边.
				answer.Add(start);

				HalfEdge opposedTriangle = start.Face.GetOpposite(current);

				Utility.Verify(opposedTriangle != null);

				Vertex opposedVertex = opposedTriangle.Next.Dest;
				if ((opposedVertex.Position - src.Position).cross2(srcDest) < 0)
				{
					current = opposedTriangle.Src;
				}
				else
				{
					current = opposedTriangle.Dest;
				}

				float cr = (opposedTriangle.Dest.Position - src.Position).cross2(srcDest);
				Utility.Verify(!MathUtility.Approximately(0, cr), "Not implement");

				List<Vertex> activeContainer = ((cr < 0) ? upperVertices : lowerVertices);

				if (!activeContainer.Contains(opposedTriangle.Dest)) { activeContainer.Add(opposedTriangle.Dest); }

				cr = (opposedTriangle.Src.Position - src.Position).cross2(srcDest);
				activeContainer = ((cr < 0) ? upperVertices : lowerVertices);

				if (!activeContainer.Contains(opposedTriangle.Src)) { activeContainer.Add(opposedTriangle.Src); }

				if (MathUtility.PointOnSegment(opposedVertex.Position, src.Position, dest.Position))
				{
					answer.Add(opposedTriangle);
					src = opposedVertex;
					break;
				}

				start = opposedTriangle;
			}

			return src;
		}

		/// <summary>
		///创建边框.
		/// </summary>
		void CreateSuperBorder(IEnumerable<Vector3> vertices)
		{
			if (!HasSuperBorder) { return; }
			float max = float.NegativeInfinity;
			foreach (Vector3 item in vertices)
			{
				max = Mathf.Max(max, Mathf.Abs(item.x), Mathf.Abs(item.z));
			}

			Vector3[] superTriangle = new Vector3[] {
				new Vector3(0, 0, 4 * max),
				new Vector3(-4 * max, 0, -4 * max),
				new Vector3(4 * max, 0, 0)
			};

			SetUpSuperTriangle(superTriangle);
			AddShape(vertices, true);
			RemoveSuperTriangle(superTriangle);
		}

		/// <summary>
		/// 移除boundingEdges组成的多边形.
		/// </summary>
		void RemoveShape(IEnumerable<HalfEdge> boundingEdges)
		{
			List<Vertex> boundingVertices = new List<Vertex>();
			foreach (HalfEdge e in boundingEdges)
			{
				boundingVertices.Add(e.Src);
			}

			List<Vertex> polygon = new List<Vertex>(boundingVertices.Count * 2);

			List<Triangle> triangles = new List<Triangle>();
			List<Vertex> vertices = new List<Vertex>();

			// 收集多边形的顶点, 以及与该顶点相关的边.
			Vertex benchmark = null;
			foreach (HalfEdge edge in boundingEdges)
			{
				foreach (HalfEdge ray in geomManager.GetRays(edge.Src))
				{
					if (ray.Face == null) { continue; }
					if (triangles.IndexOf(ray.Face) < 0) { triangles.Add(ray.Face); }

					if (boundingVertices.Contains(ray.Dest)) { continue; }
					if (polygon.Contains(ray.Dest)) { continue; }

					vertices.Add(ray.Dest);
				}

				// 构造多边形.
				vertices.Sort(new PolarAngleComparer(edge.Src.Position,
					(benchmark ?? FindBenchmark(vertices, triangles)).Position));

				if (vertices.Count > 0)
				{
					benchmark = vertices.back();
				}

				polygon.AddRange(vertices);
				vertices.Clear();
			}

			triangles.ForEach(t => { geomManager.ReleaseTriangle(t); });
			
			// 将该多边形区域三角形化.
			CreateTriangles(PolygonTriangulation.Triangulate(polygon));
		}

		/// <summary>
		/// 创建vertices中的vertices.Count/3个的三角形.
		/// </summary>
		void CreateTriangles(List<Vertex> vertices)
		{
			for (int i = 0; i < vertices.Count; i += 3)
			{
				geomManager.CreateTriangle(vertices[i], vertices[i + 1], vertices[i + 2]);
			}
		}

		/// <summary>
		/// 查找ray的环中, 与src->dest的边相交的边.
		/// </summary>
		bool FindCrossedEdge(out Tuple2<HalfEdge, CrossState> answer, HalfEdge ray, Vector3 src, Vector3 dest)
		{
			List<HalfEdge> cycle = ray.Cycle;
			answer = new Tuple2<HalfEdge, CrossState>();

			foreach (HalfEdge edge in cycle)
			{
				Vector3 point;
				CrossState crossState = MathUtility.GetLineCrossPoint(out point,
					edge.Src.Position, edge.Dest.Position,
					src, dest
				);

				if (crossState == CrossState.FullyOverlaps
					|| (crossState == CrossState.CrossOnSegment && !point.equals2(edge.Src.Position) && !point.equals2(edge.Dest.Position)))
				{
					answer.Second = crossState;
					answer.First = edge;
					if (crossState == CrossState.FullyOverlaps
						&& (dest.equals2(edge.Src.Position) || src.equals2(edge.Dest.Position)))
					{
						answer.First = answer.First.Pair;
					}

					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// 创建用于步进法生成delaunay三角划分的超级三角形.
		/// </summary>
		void SetUpSuperTriangle(Vector3[] super)
		{
			geomManager.CreateTriangle(
				geomManager.CreateVertex(super[0]),
				geomManager.CreateVertex(super[1]),
				geomManager.CreateVertex(super[2])
			);
		}

		/// <summary>
		/// 移除超级三角形.
		/// </summary>
		void RemoveSuperTriangle(Vector3[] super)
		{
			geomManager.AllTriangles.ForEach(triangle =>
			{
				if (triangle.HasVertex(super[0]) || triangle.HasVertex(super[1]) || triangle.HasVertex(super[2]))
				{
					geomManager.ReleaseTriangle(triangle);
				}
			});
		}

		/// <summary>
		/// 将点插入到三角形中.
		/// </summary>
		void InsertToTriangle(Vertex v, Triangle old)
		{
			Triangle ab = geomManager.CreateTriangle();
			Triangle bc = geomManager.CreateTriangle();
			Triangle ca = geomManager.CreateTriangle();

			// 分别构造连接v和old的三个顶点的边, 将old一分为三.
			HalfEdge av = geomManager.CreateEdge(old.A, v);
			HalfEdge bv = geomManager.CreateEdge(old.B, v);
			HalfEdge cv = geomManager.CreateEdge(old.C, v);

			HalfEdge AB = old.AB, BC = old.BC, CA = old.CA;

			// 更新Face.
			AB.Face = bv.Face = av.Pair.Face = ab;
			BC.Face = cv.Face = bv.Pair.Face = bc;
			CA.Face = av.Face = cv.Pair.Face = ca;

			// 释放旧三角形.
			geomManager.ReleaseTriangle(old);

			// 连接边.
			ab.Edge = AB.CycleLink(bv, av.Pair);
			bc.Edge = BC.CycleLink(cv, bv.Pair);
			ca.Edge = CA.CycleLink(av, cv.Pair);

			// 映射到格子地图上.
			geomManager.RasterizeTriangle(ab);
			geomManager.RasterizeTriangle(bc);
			geomManager.RasterizeTriangle(ca);

			Utility.Verify(av.Face == ca);
			Utility.Verify(av.Pair.Face == ab);

			Utility.Verify(bv.Face == ab);
			Utility.Verify(bv.Pair.Face == bc);

			Utility.Verify(cv.Face == bc);
			Utility.Verify(cv.Pair.Face == ca);

			// 维护delaunay特性.
			FlipTriangles(ab.Edge);
			FlipTriangles(bc.Edge);
			FlipTriangles(ca.Edge);
		}

		/// <summary>
		/// 将点插入到old的边上.
		/// </summary>
		void InsertOnEdge(Vertex v, Triangle old, HalfEdge hitEdge)
		{
			// 连接v和v所在的边正对的顶点, 将old一分为二.
			Triangle split1 = geomManager.CreateTriangle();
			Triangle split2 = geomManager.CreateTriangle();

			Vertex opositeVertex = hitEdge.Next.Dest;

			HalfEdge ov = geomManager.CreateEdge(opositeVertex, v);
			HalfEdge v1 = geomManager.CreateEdge(v, hitEdge.Dest);
			HalfEdge v2 = geomManager.CreateEdge(v, hitEdge.Pair.Dest);

			HalfEdge sp1Edge0 = hitEdge.Next;

			HalfEdge sp2Edge0 = hitEdge.Next.Next;
			HalfEdge sp2Edge1 = v2.Pair;
			HalfEdge sp2Edge2 = ov.Pair;

			// 更新Face.
			sp1Edge0.Face = ov.Face = v1.Face = split1;
			sp2Edge0.Face = sp2Edge1.Face = sp2Edge2.Face = split2;

			// 释放旧三角形.
			geomManager.ReleaseTriangle(old);

			// 连接边.
			split1.Edge = sp1Edge0.CycleLink(ov, v1);
			split2.Edge = sp2Edge0.CycleLink(sp2Edge1, sp2Edge2);

			// 将新三角形映射到格子地图上.
			geomManager.RasterizeTriangle(split1);
			geomManager.RasterizeTriangle(split2);

			Utility.Verify(ov.Face == split1);
			Utility.Verify(ov.Pair.Face == split2);

			Triangle other = hitEdge.Pair.Face;

			Triangle oposite1 = null;
			Triangle oposite2 = null;

			// 分割另一侧的三角形.
			if (other != null)
			{
				Vertex p = hitEdge.Pair.Next.Dest;

				HalfEdge vp = geomManager.CreateEdge(v, p);

				oposite1 = geomManager.CreateTriangle();
				oposite2 = geomManager.CreateTriangle();

				HalfEdge hpn = hitEdge.Pair.Next;
				HalfEdge op1Edge0 = hpn.Next;
				HalfEdge op1Edge1 = v1.Pair;
				HalfEdge op1Edge2 = vp;

				hpn.Face = vp.Pair.Face = v2.Face = oposite2;
				op1Edge0.Face = op1Edge1.Face = op1Edge2.Face = oposite1;
				geomManager.ReleaseTriangle(other);

				oposite2.Edge = hpn.CycleLink(vp.Pair, v2);
				oposite1.Edge = op1Edge0.CycleLink(op1Edge1, op1Edge2);

				geomManager.RasterizeTriangle(oposite1);
				geomManager.RasterizeTriangle(oposite2);

				Utility.Verify(vp.Face == oposite1);
				Utility.Verify(vp.Pair.Face == oposite2);
			}

			Utility.Verify(v1.Face == split1);
			Utility.Verify(v1.Pair.Face == oposite1);

			Utility.Verify(v2.Face == oposite2);
			Utility.Verify(v2.Pair.Face == split2);

			// 维护delaunay特性.
			FlipTriangles(split1.Edge);
			FlipTriangles(split2.Edge);

			if (other != null)
			{
				FlipTriangles(oposite1.Edge);
				FlipTriangles(oposite2.Edge);
			}
		}

		/// <summary>
		/// 检查halfEdge两侧的三角形是否满足delaunay性质. 如果不满足, 进行翻转.
		/// </summary>
		void FlipTriangles(HalfEdge halfEdge)
		{
			Stack<HalfEdge> stack = new Stack<HalfEdge>();
			stack.Push(halfEdge);

			for (; stack.Count != 0; )
			{
				halfEdge = stack.Pop();

				// 如果该边是约束边, 不翻转.
				if (halfEdge.Constrained || halfEdge.Pair.Constrained) { continue; }

				Triangle x = halfEdge.Face;
				Triangle y = halfEdge.Pair.Face;

				if (x == null || y == null) { continue; }

				Utility.Verify(x.Walkable && y.Walkable, "Can not flip unwalkable triangle");

				// 检查是否满足delaunay性质.
				if (!MathUtility.PointInCircumCircle(x.A.Position, x.B.Position, x.C.Position, halfEdge.Pair.Next.Dest.Position))
				{
					continue;
				}

				// 创建新的边.
				HalfEdge ab = geomManager.CreateEdge(halfEdge.Next.Dest, halfEdge.Pair.Next.Dest);

				HalfEdge bEdges0 = halfEdge.Pair.Next.Next;
				HalfEdge bEdges1 = halfEdge.Next;
				HalfEdge bEdges2 = ab;

				// 去掉映射.
				// 注意这里不删除三角形, 而是通过设置边, 将旧三角形改造为新的.
				geomManager.UnrasterizeTriangle(x);
				geomManager.UnrasterizeTriangle(y);

				// 连接新的边.
				x.Edge = halfEdge.Next.Next.CycleLink(halfEdge.Pair.Next, ab.Pair);
				y.Edge = bEdges0.CycleLink(bEdges1, bEdges2);

				// 映射新三角形.
				geomManager.RasterizeTriangle(x);
				geomManager.RasterizeTriangle(y);

				x.BoundingEdges.ForEach(item => { item.Face = x; });
				y.BoundingEdges.ForEach(item => { item.Face = y; });

				// 翻转之后, 检查这2个三角形的另外2条边, 是否满足delaunay性质.
				if (GuardedPushStack(stack, halfEdge.Pair.Next.Next)
					&& GuardedPushStack(stack, halfEdge.Next)
					&& GuardedPushStack(stack, halfEdge.Pair.Next)
					&& GuardedPushStack(stack, halfEdge.Next.Next))
				{
				}

				halfEdge.Face = halfEdge.Pair.Face = null;

				// 该边已不存在, 将他删除.
				geomManager.ReleaseEdge(halfEdge);
			}
		}

		/// <summary>
		/// 入栈.
		/// </summary>
		bool GuardedPushStack(Stack<HalfEdge> stack, HalfEdge item)
		{
			if (stack.Count < EditorConstants.kMaxStackCapacity)
			{
				stack.Push(item);
				return true;
			}

			return false;
		}
	}
}
