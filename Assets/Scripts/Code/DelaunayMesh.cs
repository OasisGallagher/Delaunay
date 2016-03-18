using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Delaunay
{
	class CrossResult
	{
		public HalfEdge edge;
		public CrossState crossState = CrossState.Parallel;
	}

	public class DelaunayMesh
	{
		Vector3 stAPosition;
		Vector3 stBPosition;
		Vector3 stCPosition;

		public DelaunayMesh(Rect bound)
		{
			float max = Mathf.Max(bound.xMax, bound.yMax);

			stAPosition = new Vector3(0, 0, 4 * max);
			stBPosition = new Vector3(-4 * max, 0, -4 * max);
			stCPosition = new Vector3(4 * max, 0, 0);
		}

		public void __tmpStart() { SetUpBounds(); }
		public void __tmpStop() { RemoveBounds(); }

		public void AddBorder(IEnumerable<Vector3> vertices)
		{
			AddPolygon(vertices);
		}

		public Obstacle AddObstacle(IEnumerable<Vector3> vertices)
		{
			List<HalfEdge> polygonBoundingEdges = AddPolygon(vertices);
			Obstacle obstacle = Obstacle.Create(polygonBoundingEdges);
			MarkObstacle(obstacle);
			return obstacle;
		}

		public void Clear() { GeomManager.Clear(); }

		class ContainedFacet
		{
			public int hitEdge = -1;
			public Triangle triangle = null;
		}

		public List<Vector3> FindPath(Vector3 start, Vector3 dest, float radius)
		{
			Tuple2<int, Triangle> findResult = GeomManager.FindVertexContainedTriangle(start);
			if (findResult.Second != null && !findResult.Second.Walkable)
			{
				int fromTriangleID = findResult.Second.ID;
				findResult.Second = FindWalkableTriangle(findResult.Second.FindVertex(start));
				Debug.Log(string.Format(start + "Reposition start point from triangle {0} to {1} ", fromTriangleID, findResult.Second.ID));
			}
			Utility.Verify(findResult.Second != null, "Invalid start point");
			Triangle facet1 = findResult.Second;

			findResult = GeomManager.FindVertexContainedTriangle(dest);
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
			return hit;
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

		public void RemoveObstacle(int obstacleID)
		{
			Obstacle obstacle = GeomManager.GetObstacle(obstacleID);
			List<Vertex> boundingVertices = new List<Vertex>(obstacle.BoundingEdges.Count);
			obstacle.BoundingEdges.ForEach(item => { boundingVertices.Add(item.Src); });

			List<Vertex> polygon = new List<Vertex>(obstacle.BoundingEdges.Count * 2);

			List<Triangle> triangles = new List<Triangle>();
			List<Vertex> vertices = new List<Vertex>();

			Vertex benchmark = null;
			foreach (HalfEdge edge in obstacle.BoundingEdges)
			{
				foreach (HalfEdge ray in GeomManager.GetRays(edge.Src))
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

			triangles.ForEach(t => { Triangle.Release(t); });

			CreateTriangles(PolygonTriangulation.Triangulate(polygon));
		}

		public Vector3 Raycast(Vector3 from, Vector3 to, float radius)
		{
			Tuple2<int, Triangle> result = GeomManager.FindVertexContainedTriangle(from);
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

		Triangle FindWalkableTriangle(Vertex src)
		{
			foreach (HalfEdge edge in GeomManager.GetRays(src))
			{
				if (edge.Face != null && edge.Face.Walkable)
				{
					return edge.Face;
				}
			}

			return null;
		}

		List<HalfEdge> AddPolygon(IEnumerable<Vector3> container)
		{
			IEnumerator<Vector3> e = container.GetEnumerator();
			List<HalfEdge> polygonBoundingEdges = new List<HalfEdge>();

			if (!e.MoveNext()) { return polygonBoundingEdges; }

			Vertex prevVertex = Vertex.Create(e.Current);
			Vertex firstVertex = prevVertex;

			List<Vertex> vertices = new List<Vertex>();

			for (; e.MoveNext(); )
			{
				vertices.Add(prevVertex);

				Vertex currentVertex = Vertex.Create(e.Current);
				polygonBoundingEdges.AddRange(AddConstraintEdge(prevVertex, currentVertex));
				prevVertex = currentVertex;
			}

			vertices.Add(prevVertex);
			polygonBoundingEdges.AddRange(AddConstraintEdge(prevVertex, firstVertex));

			return polygonBoundingEdges;
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

		List<Vector3> ComputeConvexHull()
		{
			List<Vector3> positions = new List<Vector3>();
			GeomManager.AllVertices.ForEach(vertex => { positions.Add(vertex.Position); });
			return ConvexHullComputer.Compute(positions);
		}

		Vertex FindBenchmark(List<Vertex> vertices, List<Triangle> triangles)
		{
			BitArray bits = new BitArray(vertices.Count);
			for (int i = 0; i < vertices.Count; ++i)
			{
				foreach (HalfEdge e in GeomManager.GetRays(vertices[i]))
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
				Triangle.Create(vertices[i], vertices[i + 1], vertices[i + 2]);
			}
		}

		List<HalfEdge> AddConstraintEdge(Vertex src, Vertex dest)
		{
			Append(src);
			Append(dest);

			List<HalfEdge> answer = new List<HalfEdge>();

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
			CrossResult crossResult = new CrossResult();
			foreach (HalfEdge ray in GeomManager.GetRays(src))
			{
				if (FindCrossedEdge(crossResult, ray, src, dest)) { break; }
			}

			Utility.Verify(crossResult.crossState != CrossState.Parallel);

			if (crossResult.crossState == CrossState.FullyOverlaps)
			{
				crossResult.edge.Constraint = true;
				src = crossResult.edge.Dest;
				return crossResult.edge;
			}

			Utility.Verify(crossResult.crossState == CrossState.CrossOnSegment);

			return ConstraintCrossEdges(ref src, dest, crossResult.edge);
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
					Triangle.Release(edge.Face);
				}

				if (edge.Pair.Face != null)
				{
					Triangle.Release(edge.Pair.Face);
				}
			}

			CreateTriangles(PolygonTriangulation.Triangulate(low, dest, src));
			CreateTriangles(PolygonTriangulation.Triangulate(up, src, dest));

			HalfEdge constraintEdge = GeomManager.GetRays(src).Find(edge => { return edge.Dest == dest; });
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

		bool FindCrossedEdge(CrossResult answer, HalfEdge ray, Vertex src, Vertex dest)
		{
			List<HalfEdge> cycle = ray.Cycle;
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
					answer.crossState = crossState;
					answer.edge = edge;
					if (crossState == CrossState.FullyOverlaps
						&& (dest.Position.equals2(edge.Src.Position) || src.Position.equals2(edge.Dest.Position)))
					{
						answer.edge = answer.edge.Pair;
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
			Tuple2<int, Triangle> answer = GeomManager.FindVertexContainedTriangle(v.Position);

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

		void SetUpBounds()
		{
			Clear();
			Triangle.Create(Vertex.Create(stAPosition), Vertex.Create(stBPosition), Vertex.Create(stCPosition));
		}

		void RemoveBounds()
		{
			GeomManager.AllTriangles.ForEach(facet =>
			{
				if (!facet.gameObject.activeSelf) { return; }
				if (facet.HasVertex(stAPosition) || facet.HasVertex(stBPosition) || facet.HasVertex(stCPosition))
				{
					Triangle.Release(facet);
				}
			});
		}

		void InsertToFacet(Vertex v, Triangle old)
		{
			Triangle ab = Triangle.Create();
			Triangle bc = Triangle.Create();
			Triangle ca = Triangle.Create();

			HalfEdge av = HalfEdge.Create(old.A, v);
			HalfEdge bv = HalfEdge.Create(old.B, v);
			HalfEdge cv = HalfEdge.Create(old.C, v);

			HalfEdge AB = old.AB, BC = old.BC, CA = old.CA;

			AB.Face = bv.Face = av.Pair.Face = ab;
			BC.Face = cv.Face = bv.Pair.Face = bc;
			CA.Face = av.Face = cv.Pair.Face = ca;

			Triangle.Release(old);

			ab.Edge = AB.CycleLink(bv, av.Pair);
			bc.Edge = BC.CycleLink(cv, bv.Pair);
			ca.Edge = CA.CycleLink(av, cv.Pair);

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
			Triangle split1 = Triangle.Create();
			Triangle split2 = Triangle.Create();

			Vertex opositeVertex = hitEdge.Next.Dest;

			HalfEdge ov = HalfEdge.Create(opositeVertex, v);
			HalfEdge v1 = HalfEdge.Create(v, hitEdge.Dest);
			HalfEdge v2 = HalfEdge.Create(v, hitEdge.Pair.Dest);

			HalfEdge sp1Edge0 = hitEdge.Next;

			HalfEdge sp2Edge0 = hitEdge.Next.Next;
			HalfEdge sp2Edge1 = v2.Pair;
			HalfEdge sp2Edge2 = ov.Pair;

			sp1Edge0.Face = ov.Face = v1.Face = split1;
			sp2Edge0.Face = sp2Edge1.Face = sp2Edge2.Face = split2;

			Triangle.Release(old);

			split1.Edge = sp1Edge0.CycleLink(ov, v1);
			split2.Edge = sp2Edge0.CycleLink(sp2Edge1, sp2Edge2);

			Utility.Verify(ov.Face == split1);
			Utility.Verify(ov.Pair.Face == split2);

			Triangle other = hitEdge.Pair.Face;

			Triangle oposite1 = null;
			Triangle oposite2 = null;

			if (other != null)
			{
				Vertex p = hitEdge.Pair.Next.Dest;

				HalfEdge vp = HalfEdge.Create(v, p);

				oposite1 = Triangle.Create();
				oposite2 = Triangle.Create();

				HalfEdge hpn = hitEdge.Pair.Next;
				HalfEdge op1Edge0 = hpn.Next;
				HalfEdge op1Edge1 = v1.Pair;
				HalfEdge op1Edge2 = vp;

				hpn.Face = vp.Pair.Face = v2.Face = oposite2;
				op1Edge0.Face = op1Edge1.Face = op1Edge2.Face = oposite1;
				Triangle.Release(other);

				oposite2.Edge = hpn.CycleLink(vp.Pair, v2);
				oposite1.Edge = op1Edge0.CycleLink(op1Edge1, op1Edge2);

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

				Triangle a = halfEdge.Face;
				Triangle b = halfEdge.Pair.Face;

				if (a == null || b == null) { continue; }

				if (!a.PointInCircumCircle(halfEdge.Pair.Next.Dest)) { continue; }

				HalfEdge ab = HalfEdge.Create(halfEdge.Next.Dest, halfEdge.Pair.Next.Dest);

				HalfEdge bEdges0 = halfEdge.Pair.Next.Next;
				HalfEdge bEdges1 = halfEdge.Next;
				HalfEdge bEdges2 = ab;

				a.Edge = halfEdge.Next.Next.CycleLink(halfEdge.Pair.Next, ab.Pair);
				b.Edge = bEdges0.CycleLink(bEdges1, bEdges2);

				a.BoundingEdges.ForEach(item => { item.Face = a; });
				b.BoundingEdges.ForEach(item => { item.Face = b; });

				if (stack.Count < EditorConstants.kMaxStackCapacity) stack.Push(halfEdge.Pair.Next.Next);

				if (stack.Count < EditorConstants.kMaxStackCapacity) stack.Push(halfEdge.Next);

				if (stack.Count < EditorConstants.kMaxStackCapacity) stack.Push(halfEdge.Pair.Next);

				if (stack.Count < EditorConstants.kMaxStackCapacity) stack.Push(halfEdge.Next.Next);

				halfEdge.Face = halfEdge.Pair.Face = null;
			}
		}
	}
}
