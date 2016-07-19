using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Delaunay
{
	public class DelaunayMesh : IPathTerrain
	{
		const float kGetNearestMaxDistance = 8;
		const int kGetNearestDistanceSampleCount = 10;
		const int kGetNearestRadianSampleCount = 8;

		List<Vector3> superBorder;
		
		public GeomManager geomManager;

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
			MeshSerializer.Load(path, geomManager, superBorder);
		}

		public void Save(string path)
		{
			MeshSerializer.Save(path, geomManager, superBorder);
		}

		public List<Vector3> FindPath(Vector3 start, Vector3 dest, float radius)
		{
			Tuple2<int, Triangle> findResult = geomManager.FindVertexContainedTriangle(start);
			if (findResult.Second != null && !findResult.Second.Walkable)
			{
				Debug.LogError("Invalid start position " + start);
				return null;
			}

			Utility.Verify(findResult.Second != null, "Invalid start point");
			Triangle facet1 = findResult.Second;

			findResult = geomManager.FindVertexContainedTriangle(dest);
			if (findResult.Second != null && !findResult.Second.Walkable)
			{
				Debug.LogError("Invalid dest position " + dest);
				return null;
			}

			Utility.Verify(findResult.Second != null, "Invalid dest point");

			return Pathfinding.FindPath(facet1, start, findResult.Second, dest, radius);
		}

		public Vector3 GetNearestPoint(Vector3 position, float radius)
		{
			if (!IsValidPosition(position, radius) && !SearchValidPosition(ref position, radius, kGetNearestMaxDistance, kGetNearestDistanceSampleCount, kGetNearestRadianSampleCount))
			{
				Debug.LogError("Failed to GetNearestPoint for " + position);
			}

			position.y = GetTerrainHeight(position);
			return position;
		}

		public Vector3 Raycast(Vector3 from, Vector3 to, float radius)
		{
			Debug.Log("Raycast");
			if (!IsValidPosition(from, radius))
			{
				Debug.Log("Stuck at " + from);
				return from;
			}

			Tuple2<int, Triangle> containedInfo = geomManager.FindVertexContainedTriangle(from);

			Utility.Verify(containedInfo.Second != null, "can not locate position " + from);

			if (containedInfo.First < 0)	// [-1, -2, -3]
			{
				Debug.LogError("Unhandled case. radius = " + radius);
				return from;
			}

			Vector3 ans = Vector3.zero;
			if (containedInfo.First == 0)
			{
				ans = RaycastFromTriangle(containedInfo.Second, from, to, radius);
				Debug.Log("~ Raycast");
				return ans;
			}

			HalfEdge edge = containedInfo.Second.GetEdgeByDirection(containedInfo.First);
			ans = RaycastFromEdge(edge, from, to, radius);
			Debug.Log("~ Raycast");
			return ans;
		}

		Vector3 RaycastFromEdge(HalfEdge edge, Vector3 from, Vector3 to, float radius)
		{
			if (from.equals2(to)) { return from; }

			if (!IsValidPosition(from, radius))
			{
				return from;
			}

			if ((to - from).cross2(edge.Dest.Position - edge.Src.Position) > 0)
			{
				edge = edge.Pair;
			}

			return RaycastWithEdges(new HalfEdge[] { edge.Next, edge.Next.Next }, from, to, radius);
		}

		Vector3 RaycastWithEdges(IEnumerable<HalfEdge> edges, Vector3 from, Vector3 to, float radius)
		{
			Debug.Log("Raycast with edges");
			Vector2 segCrossAnswer = Vector2.zero;
			foreach (HalfEdge edge in edges)
			{
				CrossState crossState = MathUtility.SegmentCross(out segCrossAnswer, from, to, edge.Src.Position, edge.Dest.Position);

				Utility.Verify(crossState == CrossState.CrossOnSegment || crossState == CrossState.CrossOnExtLine);

				if (segCrossAnswer.x < 0 || segCrossAnswer.x > 1) { continue; }

				if (crossState == CrossState.CrossOnSegment)
				{
					Vector3 cross = from + segCrossAnswer.x * (to - from);
					to = RaycastFromEdge(edge, cross, to, radius);
					break;
				}
			}

			Debug.Log("~ Raycast with edges");
			return to;
		}

		Vector3 RaycastFromTriangle(Triangle triangle, Vector3 from, Vector3 to, float radius)
		{
			if (from.equals2(to)) { return from; }

			HalfEdge[] edges = new HalfEdge[] { triangle.AB, triangle.BC, triangle.CA };
			return RaycastWithEdges(edges, from, to, radius);
		}

		public bool IsValidPosition(Vector3 position, float radius)
		{
			Tuple2<int, Triangle> containedInfo = geomManager.FindVertexContainedTriangle(position);
			if (containedInfo.Second == null || !containedInfo.Second.Walkable)
			{
				return false;
			}

			return IsValidMeshPosition(containedInfo, position, radius);
		}

		public float GetTerrainHeight(Vector3 position)
		{
			return position.y;
			/*
			Triangle triangle = geomManager.FindVertexContainedTriangle(position).Second;
			return MathUtility.LineCrossPlane(triangle.A.Position, triangle.B.Position,triangle.C.Position, position, Vector3.down).y;
			 */
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

		protected List<HalfEdge> AddShape(IEnumerable<Vector3> container, bool close)
		{
			IEnumerator<Vector3> e = container.GetEnumerator();
			List<HalfEdge> polygonBoundingEdges = new List<HalfEdge>();

			if (!e.MoveNext()) { return polygonBoundingEdges; }

			Vertex prevVertex = geomManager.CreateVertex(e.Current);
			Vertex firstVertex = prevVertex;

			for (; e.MoveNext(); )
			{
				Vertex currentVertex = geomManager.CreateVertex(e.Current);
				polygonBoundingEdges.AddRange(AddConstrainedEdge(prevVertex, currentVertex));
				prevVertex = currentVertex;
			}

			if (close)
			{
				polygonBoundingEdges.AddRange(AddConstrainedEdge(prevVertex, firstVertex));
			}

			return polygonBoundingEdges;
		}

		protected List<HalfEdge> AddConstrainedEdge(Vertex src, Vertex dest)
		{
			Append(src);
			Append(dest);

			List<HalfEdge> answer = new List<HalfEdge>();

			const int maxLoopCount = 4096;
			for (int i = 0; src != dest; )
			{
				answer.Add(AddConstrainedEdgeAt(ref src, dest));
				Utility.Verify(++i < maxLoopCount, "Max loop count exceed");
			}

			return answer;
		}

		protected HalfEdge AddConstrainedEdgeAt(ref Vertex src, Vertex dest)
		{
			Tuple2<HalfEdge, CrossState> crossResult = new Tuple2<HalfEdge, CrossState>(null, CrossState.Parallel);
			foreach (HalfEdge ray in geomManager.GetRays(src))
			{
				if (FindCrossedEdge(out crossResult, ray, src.Position, dest.Position)) { break; }
			}

			Utility.Verify(crossResult.Second != CrossState.Parallel);

			if (crossResult.Second == CrossState.FullyOverlaps)
			{
				crossResult.First.Constrained = true;
				src = crossResult.First.Dest;
				return crossResult.First;
			}

			Utility.Verify(crossResult.Second == CrossState.CrossOnSegment);

			return OnConstrainedEdgeCrossEdges(ref src, dest, crossResult.First);
		}

		protected void MarkObstacle(Obstacle obstacle)
		{
			obstacle.Mesh.ForEach(triangle => { triangle.Walkable = false; });
		}

		protected bool Append(Vertex v)
		{
			Tuple2<int, Triangle> answer = geomManager.FindVertexContainedTriangle(v.Position);

			if (answer.First < 0) { return false; }

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

		bool IsValidMeshPosition(Tuple2<int, Triangle> containedInfo, Vector3 position, float radius)
		{
			List<HalfEdge> edges = new List<HalfEdge>();
			if (containedInfo.First < 0) { return false; }

			if (containedInfo.First == 0)
			{
				edges.Add(containedInfo.Second.AB);
				edges.Add(containedInfo.Second.BC);
				edges.Add(containedInfo.Second.CA);
			}
			else
			{
				edges.Add(containedInfo.Second.GetEdgeByDirection(containedInfo.First));
				edges.Add(edges.back().Pair);
			}

			for (; edges.Count != 0; )
			{
				HalfEdge current = edges.popBack();
				if (MathUtility.PointInCircle(current.Src.Position, position, radius)
					|| MathUtility.PointInCircle(current.Dest.Position, position, radius))
				{
					return false;
				}

				// Line cross circle.
				if (MathUtility.MinDistance2Segment(position, current.Src.Position, current.Dest.Position) >= radius)
				{
					continue;
				}

				// Cross constrained edge.
				if (current.Constrained || current.Pair.Constrained)
				{
					return false;
				}

				if (current.Pair.Face == null) { continue; }

				edges.Add(current.Pair.Next);
				edges.Add(current.Pair.Next.Next);
			}

			return true;
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

		HalfEdge OnConstrainedEdgeCrossEdges(ref Vertex src, Vertex dest, HalfEdge crossedEdge)
		{
			List<Vertex> upper = new List<Vertex>();
			List<Vertex> lower = new List<Vertex>();
			List<HalfEdge> edges = new List<HalfEdge>();

			Vertex newSrc = CollectCrossedUnconstrainedEdges(edges, lower, upper, src, dest, crossedEdge);

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

			CreateTriangles(PolygonTriangulation.Triangulate(lower, dest, src));
			CreateTriangles(PolygonTriangulation.Triangulate(upper, src, dest));

			HalfEdge constrainedEdge = geomManager.GetRays(src).Find(edge => { return edge.Dest == dest; });
			Utility.Verify(constrainedEdge != null);
			constrainedEdge.Constrained = true;

			src = newSrc;
			return constrainedEdge;
		}

		Vector3 CollectCrossedUnconstrainedEdges(List<HalfEdge> answer, Vector3 src, Vector3 dest, float radius)
		{
			Vector3 srcDest = dest - src;

			HalfEdge first = null;

			for (; !first.Face.Contains(dest); )
			{
				Utility.Verify(!first.Constrained, "Crossed constrained edge");
				answer.Add(first);

				HalfEdge opposedTriangle = first.Pair;

				Utility.Verify(opposedTriangle != null);

				Vertex opposedVertex = opposedTriangle.Next.Dest;
				if ((opposedVertex.Position - src).cross2(srcDest) < 0)
				{
					first = opposedTriangle.Next;
				}
				else
				{
					first = opposedTriangle.Next.Next;
				}

				if (MathUtility.PointOnSegment(opposedVertex.Position, src, dest))
				{
					answer.Add(opposedTriangle);
					src = opposedVertex.Position;
					break;
				}
			}

			return src;
		}

		Vertex CollectCrossedUnconstrainedEdges(List<HalfEdge> answer, List<Vertex> lowerVertices, List<Vertex> upperVertices, Vertex src, Vertex dest, HalfEdge start)
		{
			Vector3 srcDest = dest.Position - src.Position;

			Vertex current = src;
			for (; !start.Face.Contains(dest.Position); )
			{
				Utility.Verify(!start.Constrained, "Crossed constrained edge");
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

		void CreateTriangles(List<Vertex> vertices)
		{
			for (int i = 0; i < vertices.Count; i += 3)
			{
				geomManager.CreateTriangle(vertices[i], vertices[i + 1], vertices[i + 2]);
			}
		}

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
				if (halfEdge.Constrained || halfEdge.Pair.Constrained) { continue; }

				Triangle x = halfEdge.Face;
				Triangle y = halfEdge.Pair.Face;

				if (x == null || y == null) { continue; }

				Utility.Verify(x.Walkable && y.Walkable, "Can not flip unwalkable triangle");

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

				if (GuardedPushStack(stack, halfEdge.Pair.Next.Next)
					&& GuardedPushStack(stack, halfEdge.Next)
					&& GuardedPushStack(stack, halfEdge.Pair.Next)
					&& GuardedPushStack(stack, halfEdge.Next.Next))
				{
				}

				halfEdge.Face = halfEdge.Pair.Face = null;
				geomManager.ReleaseEdge(halfEdge);
			}
		}

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
