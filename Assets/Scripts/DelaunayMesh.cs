using System;
using System.Collections.Generic;
using UnityEngine;

namespace Delaunay
{
	class CrossResult
	{
		public HalfEdge edge;
		public LineCrossState crossState = LineCrossState.Parallel;
	}

	public class DelaunayMesh
	{
		List<Vector3> borderCorners = new List<Vector3>();
		List<Vector3> convexHull = new List<Vector3>();

		Vector3 stAPosition;
		Vector3 stBPosition;
		Vector3 stCPosition;

		public DelaunayMesh(Rect bound)
		{
			const float padding = 0f;

			float left = bound.xMin - padding;
			float top = bound.yMax + padding;
			float right = bound.xMax + padding;
			float bottom = bound.yMin - padding;

			borderCorners.Add(new Vector3(right, 0, top));	// Right top.
			borderCorners.Add(new Vector3(left, 0, top));	// Left top.
			borderCorners.Add(new Vector3(left, 0, bottom));// Left bottom.
			borderCorners.Add(new Vector3(right, 0, bottom));// Right bottom.

			float max = Mathf.Max(bound.xMax, bound.yMax);

			stAPosition = new Vector3(0, 0, 4 * max);
			stBPosition = new Vector3(-4 * max, 0, -4 * max);
			stCPosition = new Vector3(4 * max, 0, 0);

			Rebuild();
		}

		public void Rebuild()
		{
			SetUpBounds();

			Vector3[] localCircle = new Vector3[6];
			float deltaRadian = 2 * Mathf.PI / localCircle.Length;
			for (int i = 0; i < localCircle.Length; ++i)
			{
				localCircle[i].Set(Mathf.Cos(i * deltaRadian), 0, Mathf.Sin(i * deltaRadian));
			}

			Vector3[] circle = new Vector3[localCircle.Length];

			//AddObject(localCircle.transform(circle, item => { return item * 1.5f + new Vector3(2, stAPosition.y, 0); }), true);
			AddObject(localCircle.transform(circle, item => { return item * 1.5f + new Vector3(2, stAPosition.y, 0); }), true);
			AddObject(localCircle.transform(circle, item => { return item * 1.5f + new Vector3(-2, stAPosition.y, 0); }), true);
			AddObject(localCircle.transform(circle, item => { return item * 1.5f + new Vector3(-6, stAPosition.y, 0); }), true);
			AddObject(localCircle.transform(circle, item => { return item * 1.5f + new Vector3(6, stAPosition.y, 0); }), true);

			Vector3[] localSquare = new Vector3[4];
			localSquare[0] = new Vector3(0.5f, 0f, 0.5f);
			localSquare[1] = new Vector3(-0.5f, 0f, 0.5f);
			localSquare[2] = new Vector3(-0.5f, 0f, -0.5f);
			localSquare[3] = new Vector3(0.5f, 0f, -0.5f);
			Vector3[] square = new Vector3[localSquare.Length];
			AddObject(localSquare.transform(square, item => { return item * 2f + new Vector3(-2, stAPosition.y, 5); }), true);
			AddObject(localSquare.transform(square, item => { return item * 2f + new Vector3(-2, stAPosition.y, -5); }), true);
			AddObject(localSquare.transform(square, item => { return item * 2f + new Vector3(2, stAPosition.y, 5); }), true);
			AddObject(localSquare.transform(square, item => { return item * 2f + new Vector3(2, stAPosition.y, -5); }), true);
			AddObject(localSquare.transform(square, item => { return item * 2f + new Vector3(-6, stAPosition.y, 5); }), true);
			AddObject(localSquare.transform(square, item => { return item * 2f + new Vector3(-6, stAPosition.y, -5); }), true);
			AddObject(localSquare.transform(square, item => { return item * 2f + new Vector3(6, stAPosition.y, 5); }), true);
			AddObject(localSquare.transform(square, item => { return item * 2f + new Vector3(6, stAPosition.y, -5); }), true);

			AddObject(borderCorners, false);

			RemoveBounds();

			List<Vector3> positions = new List<Vector3>();
			GeomManager.AllVertices.ForEach(vertex => { positions.Add(vertex.Position); });
			convexHull = ConvexHullComputer.Compute(positions);
		}

		class FindContainingFacetResult
		{
			public int hitEdge = 0;
			public Triangle triangle = null;
			public List<Triangle> path = null;
		}

		public List<Vector3> FindPath(Vector3 start, Vector3 dest)
		{
			// TODO: Fix from and to.
			FindContainingFacetResult findResult = FindFacetContainsVertex(start);
			Utility.Verify(findResult != null, "invalid from position " + start);
			Triangle facet1 = findResult.triangle;

			findResult = FindFacetContainsVertex(dest, facet1);
			Utility.Verify(findResult != null, "invalid to position " + dest);

			return Pathfinding.FindPath(start,dest, facet1, findResult.triangle);
		}

		public void OnDrawGizmos(bool showConvexHull)
		{
			GeomManager.AllTriangles.ForEach(facet =>
			{
				if (!facet.gameObject.activeSelf) { return; }

				facet.BoundingEdges.ForEach(edge =>
				{
					Vector3 offset = EditorConstants.kEdgeGizmosOffset;
					Debug.DrawLine(edge.Src.Position + offset,
						edge.Dest.Position + offset,
						edge.Constraint ? Color.red : Color.white
					);
				});
			});
			
			if (showConvexHull)
			{
				DrawConvexHull();
			}
		}

		void AddObject(IEnumerable<Vector3> container, bool obstacle)
		{
			IEnumerator<Vector3> e = container.GetEnumerator();
			if (!e.MoveNext()) { return; }

			Vertex prevVertex = Vertex.Create(e.Current);
			Vertex firstVertex = prevVertex;

			List<Vertex> vertices = new List<Vertex>();

			for (; e.MoveNext(); )
			{
				vertices.Add(prevVertex);

				Vertex currentVertex = Vertex.Create(e.Current);
				AddConstraintEdge(prevVertex, currentVertex);
				prevVertex = currentVertex;
			}

			vertices.Add(prevVertex);
			AddConstraintEdge(prevVertex, firstVertex);

			if (obstacle)
			{
				MarkObstacle(vertices);
			}
		}

		void AddConstraintEdge(Vertex src, Vertex dest)
		{
			Append(src);
			/*var e364 = GeomManager.AllEdges.Find(item => { return item.ID == 364; });
			if (e364 != null)
			{
				UnityEngine.Debug.Log("hit");
			}*/
			Append(dest);

			const int maxLoopCount = 4096;
			for (int i = 0; src != dest; )
			{
				src = AddConstraintAt(src, dest);
				Utility.Verify(++i < maxLoopCount, "Max loop count exceed");
			}
		}

		Vertex AddConstraintAt(Vertex src, Vertex dest)
		{
			CrossResult crossResult = new CrossResult();
			foreach (HalfEdge ray in GeomManager.GetRays(src))
			{
				if (FindCrossedEdge(crossResult, ray, src, dest)) { break; }
			}

			Utility.Verify(crossResult.crossState != LineCrossState.Parallel);

			if (crossResult.crossState == LineCrossState.FullyOverlaps)
			{
				crossResult.edge.Constraint = true;
				return crossResult.edge.Dest;
			}

			Utility.Verify(crossResult.crossState == LineCrossState.CrossOnSegment);

			List<Vertex> up = new List<Vertex>();
			List<Vertex> low = new List<Vertex>();
			List<HalfEdge> crossedEdges = new List<HalfEdge>();

			Vertex newSrc = CollectCrossedTriangles(crossedEdges, up, low, src, dest, crossResult.edge);

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

			TriangulatePseudopolygonDelaunay(low, dest, src);
			TriangulatePseudopolygonDelaunay(up, src, dest);

			HalfEdge constraintEdge = GeomManager.GetRays(src).Find(edge => { return edge.Dest == dest; });
			Utility.Verify(constraintEdge != null);
			constraintEdge.Constraint = true;

			return newSrc;
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

				if (Utility.PointOnSegment(opposedVertex.Position, src.Position, dest.Position))
				{
					crossedTriangles.Add(opposedTriangle);
					src = opposedVertex;
					break;
				}

				start = opposedTriangle;
			}

			return src;
		}

		void TriangulatePseudopolygonDelaunay(List<Vertex> vertices, Vertex src, Vertex dest)
		{
			if (vertices.Count == 0) { return; }

			Vertex c = vertices[0];
			if (vertices.Count > 1)
			{
				foreach (Vertex v in vertices)
				{
					if (Utility.PointInCircumCircle(src, dest, c, v))
					{
						c = v;
					}
				}

				List<Vertex> left = new List<Vertex>();
				List<Vertex> right = new List<Vertex>();
				List<Vertex> current = left;

				foreach (Vertex v in vertices)
				{
					if (v == c)
					{
						current = right;
						continue;
					}

					current.Add(v);
				}

				TriangulatePseudopolygonDelaunay(left, src, c);
				TriangulatePseudopolygonDelaunay(right, c, dest);
			}

			if (vertices.Count > 0)
			{
				Triangle.Create(src, dest, c);
			}
		}

		bool FindCrossedEdge(CrossResult answer, HalfEdge ray, Vertex src, Vertex dest)
		{
			List<HalfEdge> cycle = ray.Cycle;
			foreach (HalfEdge edge in cycle)
			{
				Vector3 point;
				LineCrossState crossState = Utility.GetLineCrossPoint(out point,
					edge.Src.Position, edge.Dest.Position,
					src.Position, dest.Position
				);

				if (crossState == LineCrossState.FullyOverlaps
					|| (crossState == LineCrossState.CrossOnSegment && !point.equals2(edge.Src.Position) && !point.equals2(edge.Dest.Position)))
				{
					answer.crossState = crossState;
					answer.edge = edge;
					if (crossState == LineCrossState.FullyOverlaps 
						&& (dest.Position.equals2(edge.Src.Position) || src.Position.equals2(edge.Dest.Position)))
					{
						answer.edge = answer.edge.Pair;
					}

					return true;
				}
			}

			return false;
		}

		void MarkObstacle(List<Vertex> vertices)
		{
			List<Vector3> positions = new List<Vector3>();
			vertices.ForEach(item => { positions.Add(item.Position); });

			foreach(Triangle triangle in GeomManager.AllTriangles)
			{
				if (Utility.PolygonContains(positions, triangle.A.Position)
					&& Utility.PolygonContains(positions, triangle.B.Position)
					&& Utility.PolygonContains(positions, triangle.C.Position))
				{
					triangle.Walkable = false;
				}
			}
		}

		void DrawConvexHull()
		{
			if (convexHull == null) { return; }

			for (int i = 1; i < convexHull.Count; ++i)
			{
				Vector3 prev = convexHull[i - 1];
				Vector3 current = convexHull[i];
				prev.y = current.y = EditorConstants.kConvexHullGizmosHeight;
				Debug.DrawLine(prev, current, Color.green);
			}

			if (convexHull.Count >= 2)
			{
				Vector3 prev = convexHull[(convexHull.Count - 1) % convexHull.Count];
				Vector3 current = convexHull[0];
				prev.y = current.y = EditorConstants.kConvexHullGizmosHeight;
				Debug.DrawLine(prev, current, Color.green);
			}
		}

		bool Append(Vertex v)
		{
			FindContainingFacetResult answer = FindFacetContainsVertex(v.Position);

			if (answer == null) { return false; }

			Utility.Verify(answer.hitEdge >= 0);

			if (answer.hitEdge == 0)
			{
				InsertToFacet(v, answer.triangle);
			}
			else
			{
				HalfEdge hitEdge = Utility.GetHalfEdgeByDirection(answer.triangle, answer.hitEdge);
				InsertOnEdge(v, answer.triangle, hitEdge);
			}

			return true;
		}

		void SetUpBounds()
		{
			GeomManager.Clear();
			Triangle.Create(Vertex.Create(stAPosition), Vertex.Create(stBPosition), Vertex.Create(stCPosition));
		}

		void RemoveBounds()
		{
			GeomManager.AllTriangles.ForEach(facet =>
			{
				if (facet.HasVertex(stAPosition) || facet.HasVertex(stBPosition) || facet.HasVertex(stCPosition))
				{
					Triangle.Release(facet);
				}
			});
		}

		FindContainingFacetResult FindFacetContainsVertex(Vector3 position, Triangle startFacet = null)
		{
			FindContainingFacetResult answer = new FindContainingFacetResult();

			startFacet = startFacet ?? GeomManager.AllTriangles[0];

			for (; startFacet != null; )
			{
				if (answer.path != null) { answer.path.Add(startFacet); }
				if (startFacet.HasVertex(position))
				{
					return null;
				}

				int iedge = startFacet.GetPointDirection(position);
				if (iedge >= 0)
				{
					answer.hitEdge = iedge;
					answer.triangle = startFacet;
					return answer;
				}

				startFacet = Utility.GetHalfEdgeByDirection(startFacet, iedge).Pair.Face;
			}

			return null;
		}

		void InsertToFacet(Vertex v, Triangle old)
		{
			Triangle ab = Triangle.Create(old);
			Triangle bc = Triangle.Create(old);
			Triangle ca = Triangle.Create(old);

			HalfEdge av = HalfEdge.Create(old.A, v);
			HalfEdge bv = HalfEdge.Create(old.B, v);
			HalfEdge cv = HalfEdge.Create(old.C, v);

			HalfEdge AB = old.AB, BC = old.BC, CA = old.CA;

			AB.Face = bv.Face = av.Pair.Face = ab;
			BC.Face = cv.Face = bv.Pair.Face = bc;
			CA.Face = av.Face = cv.Pair.Face = ca;

			Triangle.Release(old);

			ab.Edge = Utility.CycleLink(AB, bv, av.Pair);
			//ab.BoundingEdges.ForEach(item => { item.Face = ab; });
			
			bc.Edge = Utility.CycleLink(BC, cv, bv.Pair);
			//bc.BoundingEdges.ForEach(item => { item.Face = bc; });
			
			ca.Edge = Utility.CycleLink(CA, av, cv.Pair);
			//ca.BoundingEdges.ForEach(item => { item.Face = ca; });

			Utility.Verify(av.Face == ca);
			Utility.Verify(av.Pair.Face == ab);
			//av.Face = ca;
			//av.Pair.Face = ab;

			Utility.Verify(bv.Face == ab);
			Utility.Verify(bv.Pair.Face == bc);
			//bv.Face = ab;
			//bv.Pair.Face = bc;

			Utility.Verify(cv.Face == bc);
			Utility.Verify(cv.Pair.Face == ca);
			//cv.Face = bc;
			//cv.Pair.Face = ca;

			FlipIfNeeded(ab.Edge);
			FlipIfNeeded(bc.Edge);
			FlipIfNeeded(ca.Edge);
		}

		void InsertOnEdge(Vertex v, Triangle old, HalfEdge hitEdge)
		{
			Triangle split1 = Triangle.Create(old);
			Triangle split2 = Triangle.Create(old);

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

			split1.Edge = Utility.CycleLink(sp1Edge0, ov, v1);
			//split1.BoundingEdges.ForEach(item => { item.Face = split1; });

			split2.Edge = Utility.CycleLink(sp2Edge0, sp2Edge1, sp2Edge2);
			//split2.BoundingEdges.ForEach(item => { item.Face = split2; });

			Utility.Verify(ov.Face == split1);
			Utility.Verify(ov.Pair.Face == split2);
			//ov.Face = split1;
			//ov.Pair.Face = split2;

			Triangle other = hitEdge.Pair.Face;

			Triangle oposite1 = null;
			Triangle oposite2 = null;

			if (other != null)
			{
				Vertex p = hitEdge.Pair.Next.Dest;

				HalfEdge vp = HalfEdge.Create(v, p);

				oposite1 = Triangle.Create(other);
				oposite2 = Triangle.Create(other);

				HalfEdge hpn = hitEdge.Pair.Next;
				HalfEdge op1Edge0 = hpn.Next;
				HalfEdge op1Edge1 = v1.Pair;
				HalfEdge op1Edge2 = vp;

				hpn.Face = vp.Pair.Face = v2.Face = oposite2;
				op1Edge0.Face = op1Edge1.Face = op1Edge2.Face = oposite1;
				Triangle.Release(other);

				oposite2.Edge = Utility.CycleLink(hpn, vp.Pair, v2);
				//oposite2.BoundingEdges.ForEach(item => { item.Face = oposite2; });

				oposite1.Edge = Utility.CycleLink(op1Edge0, op1Edge1, op1Edge2);
				//oposite1.BoundingEdges.ForEach(item => { item.Face = oposite1; });

				Utility.Verify(vp.Face == oposite1);
				Utility.Verify(vp.Pair.Face == oposite2);

				//vp.Face = oposite1;
				//vp.Pair.Face = oposite2;
			}

			Utility.Verify(v1.Face == split1);
			Utility.Verify(v1.Pair.Face == oposite1);

			//v1.Face = split1;
			//v1.Pair.Face = oposite1;

			Utility.Verify(v2.Face == oposite2);
			Utility.Verify(v2.Pair.Face == split2);

			//v2.Face = oposite2;
			//v2.Pair.Face = split2;

			//HalfEdge.Release(hitEdge);

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

				a.Edge = Utility.CycleLink(halfEdge.Next.Next, halfEdge.Pair.Next, ab.Pair);
				b.Edge = Utility.CycleLink(bEdges0, bEdges1, bEdges2);

				a.BoundingEdges.ForEach(item => { item.Face = a; });
				b.BoundingEdges.ForEach(item => { item.Face = b; });

				/*if (halfEdge.Dest.Edge == halfEdge.Pair)
				{
					Utility.FixVertexHalfEdge(halfEdge.Dest);
				}

				if (halfEdge.Pair.Dest.Edge == halfEdge)
				{
					Utility.FixVertexHalfEdge(halfEdge.Pair.Dest);
				}*/

				if (stack.Count < EditorConstants.kMaxStackCapacity) stack.Push(halfEdge.Pair.Next.Next);

				if (stack.Count < EditorConstants.kMaxStackCapacity) stack.Push(halfEdge.Next);

				if (stack.Count < EditorConstants.kMaxStackCapacity) stack.Push(halfEdge.Pair.Next);

				if (stack.Count < EditorConstants.kMaxStackCapacity) stack.Push(halfEdge.Next.Next);

				halfEdge.Face = halfEdge.Pair.Face = null;
				//HalfEdge.Release(halfEdge);
			}
		}
	}
}
