using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Delaunay
{
	public class DelaunayMesh : IPathTerrain
	{
		GeomManager geomManager;
		List<Vector3> superBorder;

		public DelaunayMesh()
		{
			geomManager = new GeomManager();
			superBorder = new List<Vector3>();
		}

		public void AddSuperBorder(IEnumerable<Vector3> vertices)
		{
			Utility.Verify(!HasSuperBorder);
			superBorder.AddRange(vertices);
			CreateSuperBorder(superBorder);
		}

		public Obstacle AddObstacle(IEnumerable<Vector3> vertices)
		{
			List<HalfEdge> polygonBoundingEdges = AddShape(vertices, true);
			Obstacle obstacle = geomManager.CreateObstacle(polygonBoundingEdges);
			MarkObstacle(obstacle);
			return obstacle;
		}

		public void RemoveObstacle(int obstacleID)
		{
			Obstacle obstacle = geomManager.GetObstacle(obstacleID);
			RemoveShape(obstacle.BoundingEdges);
			geomManager.ReleaseObstacle(obstacle);
		}

		public BorderSet AddBorderSet(IEnumerable<Vector3> vertices, bool close)
		{
			List<HalfEdge> polygonBoundingEdges = AddShape(vertices, close);
			BorderSet borderSet = geomManager.CreateBorderSet(polygonBoundingEdges);
			return borderSet;
		}

		public void RemoveBorderSet(int borderSetID)
		{
			BorderSet borderSet = geomManager.GetBorderSet(borderSetID);
			RemoveShape(borderSet.BoundingEdges);
			geomManager.ReleaseBorderSet(borderSet);
		}

		public void ClearMesh()
		{
			geomManager.Clear();
			CreateSuperBorder(superBorder);
		}

		public void ClearAll()
		{
			geomManager.Clear();
			superBorder.Clear();
		}

		public void Load(string path)
		{
			geomManager.Clear();
			SerializeTools.Load(path, geomManager, superBorder);
		}

		public void Save(string path)
		{
			SerializeTools.Save(path, geomManager, superBorder);
		}

		public List<Vector3> FindPath(Vector3 start, Vector3 dest, float radius)
		{
			Tuple2<int, Triangle> findResult = geomManager.FindVertexContainedTriangle(start);
			if (findResult.Second != null && !findResult.Second.Walkable)
			{
				int fromTriangleID = findResult.Second.ID;
				findResult.Second = FindWalkableTriangle(findResult.Second.FindVertex(start));
				Debug.Log(string.Format(start + "Reposition start point from triangle {0} to {1} ", fromTriangleID, findResult.Second.ID));
			}
			Utility.Verify(findResult.Second != null, "Invalid start point");
			Triangle facet1 = findResult.Second;

			findResult = geomManager.FindVertexContainedTriangle(dest);
			if (findResult.Second != null && !findResult.Second.Walkable)
			{
				findResult.Second = FindWalkableTriangle(findResult.Second.FindVertex(dest));
				Debug.Log("Reposition dest point to triangle " + findResult.Second.ID);
			}
			Utility.Verify(findResult.Second != null, "Invalid dest point");

			return Pathfinding.FindPath(start, dest, facet1, findResult.Second, radius);
		}

		public Vector3 GetNearestPoint(Vector3 hit, float radius)
		{
			return new Vector3(hit.x, GetTerrainHeight(hit), hit.z);
			// TODO:
			//	Triangle triangle = FindFacetContainsVertex(hit).Second;
			/*
			foreach (Obstacle obstacle in GeomManager.AllObstacles)
			{
				triangle = obstacle.Mesh.Find(item =>
				{
					return MathUtility.MinDistance(hit, item.A.Position, item.B.Position) < radius
					|| MathUtility.MinDistance(hit, item.B.Position, item.C.Position) < radius
					|| MathUtility.MinDistance(hit, item.C.Position, item.A.Position) < radius;
				});

				if (triangle != null) { break; }
			}

			if (triangle == null) { return hit; }
			 */
			//return GetNearestPoint(triangle, hit, radius);
		}

		public Vector3 Raycast(Vector3 from, Vector3 to, float radius)
		{
			Tuple2<int, Triangle> result = geomManager.FindVertexContainedTriangle(from);
			Utility.Verify(result.Second != null, "Can not find facet contains " + from);
			if (result.First < 0)
			{
				Vertex vertex = result.Second.FindVertex(from);
				HalfEdge edge = result.Second.GetOpposite(vertex);
				Vector2 crossPoint = Vector2.zero;
				CrossState crossState = MathUtility.SegmentCross(out crossPoint, from, to, edge.Src.Position, edge.Dest.Position);
			}

			return Vector3.zero;
		}

		public float GetTerrainHeight(Vector3 position)
		{
			Triangle triangle = geomManager.FindVertexContainedTriangle(position).Second;
			return MathUtility.LineCrossPlane(triangle.A.Position, triangle.B.Position,triangle.C.Position, position, Vector3.down).y;
		}

		public bool HasSuperBorder
		{
			get { return superBorder.Count > 0; }
		}

		public IEnumerable<Vector3> BorderVertices
		{
			get { return superBorder; }
		}

		public List<Vertex> AllVertices
		{
			get { return geomManager.AllVertices; }
		}

		public List<HalfEdge> AllEdges
		{
			get { return geomManager.AllEdges; }
		}

		public List<Triangle> AllTriangles
		{
			get { return geomManager.AllTriangles; }
		}

		public List<Obstacle> AllObstacles
		{
			get { return geomManager.AllObstacles; }
		}

		public TiledMap Map
		{
			get { return geomManager.Map; }
		}

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

		Triangle FindWalkableTriangle(Vertex src)
		{
			foreach (HalfEdge edge in geomManager.GetRays(src))
			{
				if (edge.Face != null && edge.Face.Walkable)
				{
					return edge.Face;
				}
			}

			return null;
		}

		List<HalfEdge> AddShape(IEnumerable<Vector3> container, bool close)
		{
			IEnumerator<Vector3> e = container.GetEnumerator();
			List<HalfEdge> polygonBoundingEdges = new List<HalfEdge>();

			if (!e.MoveNext()) { return polygonBoundingEdges; }

			Vertex prevVertex = geomManager.CreateVertex(e.Current);
			Vertex firstVertex = prevVertex;

			for (; e.MoveNext(); )
			{
				Vertex currentVertex = geomManager.CreateVertex(e.Current);
				polygonBoundingEdges.AddRange(AddConstraintEdge(prevVertex, currentVertex));
				prevVertex = currentVertex;
			}

			if (close)
			{
				polygonBoundingEdges.AddRange(AddConstraintEdge(prevVertex, firstVertex));
			}

			return polygonBoundingEdges;
		}

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

			CreateTriangles(PolygonTriangulation.Triangulate(polygon));
		}

		Vector3 GetNearestPoint(Triangle triangle, Vector3 hit, float radius)
		{
			Queue<Triangle> queue = new Queue<Triangle>();
			List<Triangle> visited = new List<Triangle>() { triangle };

			queue.Enqueue(triangle);
			Vector3 answer = Vector3.zero;

			for (; queue.Count > 0; )
			{
				triangle = queue.Dequeue();
				if (triangle.Walkable && triangle.Place(out answer, hit, radius))
				{
					break;
				}

				if (triangle.AB.Pair.Face != null && !visited.Contains(triangle.AB.Pair.Face))
				{
					queue.Enqueue(triangle.AB.Pair.Face);
					visited.Add(triangle.AB.Pair.Face);
				}

				if (triangle.BC.Pair.Face != null && !visited.Contains(triangle.BC.Pair.Face))
				{
					queue.Enqueue(triangle.BC.Pair.Face);
					visited.Add(triangle.BC.Pair.Face);
				}

				if (triangle.CA.Pair.Face != null && !visited.Contains(triangle.CA.Pair.Face))
				{
					queue.Enqueue(triangle.CA.Pair.Face);
					visited.Add(triangle.CA.Pair.Face);
				}
			}

			return answer;
		}

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

		void CreateTriangles(List<Vertex> vertices)
		{
			for (int i = 0; i < vertices.Count; i += 3)
			{
				geomManager.CreateTriangle(vertices[i], vertices[i + 1], vertices[i + 2]);
			}
		}

		public static bool __Test = false;
		List<HalfEdge> AddConstraintEdge(Vertex src, Vertex dest)
		{
			Append(src);
			Append(dest);

			List<HalfEdge> answer = new List<HalfEdge>();

			if (__Test) { return answer; }

			const int maxLoopCount = 4096;
			for (int i = 0; src != dest; )
			{
				answer.Add(AddConstraintAt(ref src, dest));
				Utility.Verify(++i < maxLoopCount, "Max loop count exceed");
			}

			return answer;
		}

		HalfEdge AddConstraintAt(ref Vertex src, Vertex dest)
		{
			Tuple2<HalfEdge, CrossState> crossResult = new Tuple2<HalfEdge, CrossState>(null, CrossState.Parallel);
			foreach (HalfEdge ray in geomManager.GetRays(src))
			{
				if (FindCrossedEdge(out crossResult, ray, src, dest)) { break; }
			}

			Utility.Verify(crossResult.Second != CrossState.Parallel);

			if (crossResult.Second == CrossState.FullyOverlaps)
			{
				crossResult.First.Constraint = true;
				src = crossResult.First.Dest;
				return crossResult.First;
			}

			Utility.Verify(crossResult.Second == CrossState.CrossOnSegment);

			return ConstraintCrossEdges(ref src, dest, crossResult.First);
		}

		HalfEdge ConstraintCrossEdges(ref Vertex src, Vertex dest, HalfEdge crossedEdge)
		{
			List<Vertex> up = new List<Vertex>();
			List<Vertex> low = new List<Vertex>();
			List<HalfEdge> crossedEdges = new List<HalfEdge>();

			Vertex newSrc = CollectCrossedTriangles(crossedEdges, up, low, src, dest, crossedEdge);

			for (int i = 0; i < crossedEdges.Count; ++i)
			{
				HalfEdge edge = crossedEdges[i];
				if (edge.Face != null)
				{
					geomManager.ReleaseTriangle(edge.Face);
				}

				if (edge.Pair.Face != null)
				{
					geomManager.ReleaseTriangle(edge.Pair.Face);
				}
			}

			CreateTriangles(PolygonTriangulation.Triangulate(low, dest, src));
			CreateTriangles(PolygonTriangulation.Triangulate(up, src, dest));

			HalfEdge constraintEdge = geomManager.GetRays(src).Find(edge => { return edge.Dest == dest; });
			Utility.Verify(constraintEdge != null);
			constraintEdge.Constraint = true;

			src = newSrc;
			return constraintEdge;
		}

		Vertex CollectCrossedTriangles(List<HalfEdge> crossedTriangles,
			List<Vertex> up, List<Vertex> low, Vertex src, Vertex dest, HalfEdge start)
		{
			Vector3 srcDest = dest.Position - src.Position;

			Vertex v = src;
			for (; !start.Face.Contains(dest); )
			{
				Utility.Verify(!start.Constraint, "Crossed constraint edge");
				crossedTriangles.Add(start);

				HalfEdge opposedTriangle = start.Face.GetOpposite(v);

				Utility.Verify(opposedTriangle != null);

				Vertex opposedVertex = opposedTriangle.Next.Dest;
				if ((opposedVertex.Position - src.Position).cross2(srcDest) < 0)
				{
					v = opposedTriangle.Src;
				}
				else
				{
					v = opposedTriangle.Dest;
				}

				float cr = (opposedTriangle.Dest.Position - src.Position).cross2(srcDest);
				Utility.Verify(!Mathf.Approximately(0, cr), "Not implement");
				List<Vertex> activeContainer = ((cr < 0) ? up : low);

				if (!activeContainer.Contains(opposedTriangle.Dest)) { activeContainer.Add(opposedTriangle.Dest); }

				cr = (opposedTriangle.Src.Position - src.Position).cross2(srcDest);
				activeContainer = ((cr < 0) ? up : low);

				if (!activeContainer.Contains(opposedTriangle.Src)) { activeContainer.Add(opposedTriangle.Src); }

				if (MathUtility.PointOnSegment(opposedVertex.Position, src.Position, dest.Position))
				{
					crossedTriangles.Add(opposedTriangle);
					src = opposedVertex;
					break;
				}

				start = opposedTriangle;
			}

			return src;
		}

		bool FindCrossedEdge(out Tuple2<HalfEdge, CrossState> answer, HalfEdge ray, Vertex src, Vertex dest)
		{
			List<HalfEdge> cycle = ray.Cycle;
			answer = new Tuple2<HalfEdge, CrossState>();

			foreach (HalfEdge edge in cycle)
			{
				Vector3 point;
				CrossState crossState = MathUtility.GetLineCrossPoint(out point,
					edge.Src.Position, edge.Dest.Position,
					src.Position, dest.Position
				);

				if (crossState == CrossState.FullyOverlaps
					|| (crossState == CrossState.CrossOnSegment && !point.equals2(edge.Src.Position) && !point.equals2(edge.Dest.Position)))
				{
					answer.Second = crossState;
					answer.First = edge;
					if (crossState == CrossState.FullyOverlaps
						&& (dest.Position.equals2(edge.Src.Position) || src.Position.equals2(edge.Dest.Position)))
					{
						answer.First = answer.First.Pair;
					}

					return true;
				}
			}

			return false;
		}

		void MarkObstacle(Obstacle obstacle)
		{
			obstacle.Mesh.ForEach(triangle => { triangle.Walkable = false; });
		}

		bool Append(Vertex v)
		{
			Tuple2<int, Triangle> answer = geomManager.FindVertexContainedTriangle(v.Position);

			if (answer.First < 0) { return false; }

			Utility.Verify(answer.First >= 0);

			if (answer.First == 0)
			{
				InsertToFacet(v, answer.Second);
			}
			else
			{
				HalfEdge hitEdge = answer.Second.GetEdgeByDirection(answer.First);
				InsertOnEdge(v, answer.Second, hitEdge);
			}

			return true;
		}

		void SetUpSuperTriangle(Vector3[] super)
		{
			geomManager.CreateTriangle(
				geomManager.CreateVertex(super[0]),
				geomManager.CreateVertex(super[1]),
				geomManager.CreateVertex(super[2])
			);
		}

		void RemoveSuperTriangle(Vector3[] super)
		{
			geomManager.AllTriangles.ForEach(facet =>
			{
				if (facet.HasVertex(super[0]) || facet.HasVertex(super[1]) || facet.HasVertex(super[2]))
				{
					geomManager.ReleaseTriangle(facet);
				}
			});
		}

		void InsertToFacet(Vertex v, Triangle old)
		{
			Triangle ab = geomManager.CreateTriangle();
			Triangle bc = geomManager.CreateTriangle();
			Triangle ca = geomManager.CreateTriangle();

			HalfEdge av = geomManager.CreateEdge(old.A, v);
			HalfEdge bv = geomManager.CreateEdge(old.B, v);
			HalfEdge cv = geomManager.CreateEdge(old.C, v);

			HalfEdge AB = old.AB, BC = old.BC, CA = old.CA;

			AB.Face = bv.Face = av.Pair.Face = ab;
			BC.Face = cv.Face = bv.Pair.Face = bc;
			CA.Face = av.Face = cv.Pair.Face = ca;

			geomManager.ReleaseTriangle(old);

			ab.Edge = AB.CycleLink(bv, av.Pair);
			bc.Edge = BC.CycleLink(cv, bv.Pair);
			ca.Edge = CA.CycleLink(av, cv.Pair);

			geomManager.RasterizeTriangle(ab);
			geomManager.RasterizeTriangle(bc);
			geomManager.RasterizeTriangle(ca);

			Utility.Verify(av.Face == ca);
			Utility.Verify(av.Pair.Face == ab);

			Utility.Verify(bv.Face == ab);
			Utility.Verify(bv.Pair.Face == bc);

			Utility.Verify(cv.Face == bc);
			Utility.Verify(cv.Pair.Face == ca);

			FlipIfNeeded(ab.Edge);
			FlipIfNeeded(bc.Edge);
			FlipIfNeeded(ca.Edge);
		}

		void InsertOnEdge(Vertex v, Triangle old, HalfEdge hitEdge)
		{
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

			sp1Edge0.Face = ov.Face = v1.Face = split1;
			sp2Edge0.Face = sp2Edge1.Face = sp2Edge2.Face = split2;

			geomManager.ReleaseTriangle(old);

			split1.Edge = sp1Edge0.CycleLink(ov, v1);
			split2.Edge = sp2Edge0.CycleLink(sp2Edge1, sp2Edge2);

			geomManager.RasterizeTriangle(split1);
			geomManager.RasterizeTriangle(split2);

			Utility.Verify(ov.Face == split1);
			Utility.Verify(ov.Pair.Face == split2);

			Triangle other = hitEdge.Pair.Face;

			Triangle oposite1 = null;
			Triangle oposite2 = null;

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

			FlipIfNeeded(split1.Edge);
			FlipIfNeeded(split2.Edge);

			if (other != null)
			{
				FlipIfNeeded(oposite1.Edge);
				FlipIfNeeded(oposite2.Edge);
			}
		}

		void FlipIfNeeded(HalfEdge halfEdge)
		{
			Stack<HalfEdge> stack = new Stack<HalfEdge>();
			stack.Push(halfEdge);

			for (; stack.Count != 0; )
			{
				halfEdge = stack.Pop();
				if (halfEdge.Constraint) { continue; }

				Triangle x = halfEdge.Face;
				Triangle y = halfEdge.Pair.Face;

				if (x == null || y == null) { continue; }

				if (!MathUtility.PointInCircumCircle(x.A.Position, x.B.Position, x.C.Position, halfEdge.Pair.Next.Dest.Position))
				{
					continue;
				}

				HalfEdge ab = geomManager.CreateEdge(halfEdge.Next.Dest, halfEdge.Pair.Next.Dest);

				HalfEdge bEdges0 = halfEdge.Pair.Next.Next;
				HalfEdge bEdges1 = halfEdge.Next;
				HalfEdge bEdges2 = ab;

				geomManager.UnrasterizeTriangle(x);
				geomManager.UnrasterizeTriangle(y);

				x.Edge = halfEdge.Next.Next.CycleLink(halfEdge.Pair.Next, ab.Pair);
				y.Edge = bEdges0.CycleLink(bEdges1, bEdges2);

				geomManager.RasterizeTriangle(x);
				geomManager.RasterizeTriangle(y);

				x.BoundingEdges.ForEach(item => { item.Face = x; });
				y.BoundingEdges.ForEach(item => { item.Face = y; });

				if (stack.Count < EditorConstants.kMaxStackCapacity) stack.Push(halfEdge.Pair.Next.Next);

				if (stack.Count < EditorConstants.kMaxStackCapacity) stack.Push(halfEdge.Next);

				if (stack.Count < EditorConstants.kMaxStackCapacity) stack.Push(halfEdge.Pair.Next);

				if (stack.Count < EditorConstants.kMaxStackCapacity) stack.Push(halfEdge.Next.Next);

				halfEdge.Face = halfEdge.Pair.Face = null;
				geomManager.ReleaseEdge(halfEdge);
			}
		}
	}
}
