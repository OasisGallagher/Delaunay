﻿using System;
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
		public List<Triangle> Facets = new List<Triangle>();

		List<Vector3> borderCorners = new List<Vector3>();
		List<Vector3> convexHull = new List<Vector3>();

		Triangle superTriangle;

		Vector3 stAPosition;
		Vector3 stBPosition;
		Vector3 stCPosition;

		List<Vector3> findBoundTrianglePath = new List<Vector3>();

		public DelaunayMesh(Rect bound)
		{
			const float padding = -2; // 0.2f;

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

			Vector3 boxCenter = new Vector3(-4, stAPosition.y, 5);
			Vector3[] box = 
			{
				boxCenter + new Vector3(-4, 0, 1),
				boxCenter + new Vector3(4, 0, 1),
				boxCenter + new Vector3(-4, 0, -1),
				boxCenter + new Vector3(4, 0, -1),
			};

			//AddObject(box);
			AddObject(borderCorners);

			RemoveBounds();

			List<Vector3> positions = new List<Vector3>();
			HalfEdgeContainer.Vertices.ForEach(vertex => { positions.Add(vertex.Position); });
			convexHull = ConvexHullComputer.Compute(positions);
		}

		public void AddConstraintEdge(Vector3 src, Vector3 dest)
		{
			Vertex vSrc = new Vertex(src), vDest = new Vertex(dest);
			Append(vSrc);
			Append(vDest);

			for (; vSrc != vDest; )
			{
				vSrc = AddConstraintAt(vSrc, vDest);
			}
		}

		public void OnDrawGizmos(bool showFindBoundTrianglePath, bool showConvexHull)
		{
			Facets.ForEach(facet =>
			{
				if (!facet.gameObject.activeSelf) { return; }

				facet.AllEdges.ForEach(edge =>
				{
					//if (!edge.Forward) { return; }
					
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

			if (showFindBoundTrianglePath)
			{
				DrawFindBoundTrianglePath();
			}
		}

		void AddObject(IEnumerable<Vector3> container)
		{
			IEnumerator<Vector3> e = container.GetEnumerator();
			if (!e.MoveNext()) { return; }
			Vector3 lastPosition = e.Current;

			for (; e.MoveNext(); )
			{
				AddConstraintEdge(lastPosition, e.Current);
				lastPosition = e.Current;
			}
		}

		Vertex AddConstraintAt(Vertex src, Vertex dest)
		{
			CrossResult crossResult = new CrossResult();
			foreach (HalfEdge ray in HalfEdgeContainer.GetRays(src))
			{
				if (FindCrossedEdge(crossResult, ray, src, dest)) { break; }
			}

			Utility.Verify(crossResult.crossState != LineCrossState.Parallel);

			if (crossResult.crossState == LineCrossState.Collinear)
			{
				crossResult.edge.Constraint = true;
				return crossResult.edge.Dest;
			}

			Utility.Verify(crossResult.crossState == LineCrossState.CrossOnSegment);

			List<Vertex> up = new List<Vertex>();
			List<Vertex> low = new List<Vertex>();
			List<HalfEdge> crossedEdges = new List<HalfEdge>();

			Vertex newSrc = CollectCrossedTriangles(crossedEdges, up, low, src, dest, crossResult.edge);
			crossedEdges.ForEach(edge =>
			{
				if (edge.Face != null)
				{
					Facets.Remove(edge.Face);
					Triangle.Release(edge.Face, true);
				}
				if (edge.Pair.Face != null)
				{
					Facets.Remove(edge.Pair.Face);
					Triangle.Release(edge.Pair.Face, true);
				}
			});

			TriangulatePseudopolygonDelaunay(low, dest, src);
			TriangulatePseudopolygonDelaunay(up, src, dest);

			HalfEdge constraintEdge = HalfEdgeContainer.GetRays(src).Find(edge => { return edge.Dest == dest; });
			Utility.Verify(constraintEdge != null);
			constraintEdge.Constraint = true;

			return newSrc;
		}

		Vertex CollectCrossedTriangles(List<HalfEdge> crossedTriangles, List<Vertex> up, List<Vertex> low, Vertex src, Vertex dest, HalfEdge start)
		{
			Vector3 srcDest = dest.Position - src.Position;

			Vertex v = src;
			for (; !start.Face.Contains(dest); )
			{
				crossedTriangles.Add(start);

				HalfEdge opposedTriangle = start.Face.GetOpposite(v);

				Utility.Verify(opposedTriangle != null);

				Vertex opposedVertex = opposedTriangle.Next.Dest;
				if (Utility.Cross2D(opposedVertex.Position - src.Position, srcDest) < 0)
				{
					v = opposedTriangle.Dest;
				}
				else
				{
					v = opposedTriangle.Src;
				}

				float cr = Utility.Cross2D(opposedTriangle.Dest.Position - src.Position, srcDest);
				Utility.Verify(!Mathf.Approximately(0, cr), "Not implement");
				List<Vertex> activeContainer = ((cr < 0) ? up : low);

				if (!activeContainer.Contains(opposedTriangle.Dest)) { activeContainer.Add(opposedTriangle.Dest); }

				cr = Utility.Cross2D(opposedTriangle.Src.Position - src.Position, srcDest);
				activeContainer = ((cr < 0) ? up : low);

				if (!activeContainer.Contains(opposedTriangle.Src)) { activeContainer.Add(opposedTriangle.Src); }

				if (Utility.PointOnSegment(opposedVertex.Position, src.Position, dest.Position))
				{
					// MODIFY src here.
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
				Facets.Add(Triangle.Create(src, dest, c));
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

				if (crossState == LineCrossState.Collinear
					|| (crossState == LineCrossState.CrossOnSegment && !Utility.Equals2D(point, edge.Src.Position) && !Utility.Equals2D(point, edge.Dest.Position)))
				{
					answer.crossState = crossState;
					answer.edge = edge;
					if (crossState == LineCrossState.Collinear && dest.Position == edge.Src.Position)
					{
						answer.edge = answer.edge.Pair;
					}

					return true;
				}
			}

			return false;
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

		void DrawFindBoundTrianglePath()
		{
			for (int i = 1; i < findBoundTrianglePath.Count; ++i)
			{
				Debug.DrawLine(findBoundTrianglePath[i - 1] + EditorConstants.kEdgeGizmosOffset * 2,
					findBoundTrianglePath[i] + EditorConstants.kEdgeGizmosOffset * 2, Color.magenta
				);
			}
		}

		bool Append(Vertex v)
		{
			List<Triangle> newFacets = new List<Triangle>();
			List<Triangle> oldFacets = new List<Triangle>();

			int iedge = 0;
			Triangle triangle = FindFacetContainsVertex(out iedge, v);

			if (triangle == null) { return false; }

			Utility.Verify(iedge >= 0);

			if (iedge == 0)
			{
				InsertToFacet(v, triangle, oldFacets, newFacets);
			}
			else
			{
				HalfEdge hitEdge = Utility.GetHalfEdgeByDirection(triangle, iedge);
				InsertOnEdge(v, triangle, hitEdge, oldFacets, newFacets);
			}

			// Simply remove old facets from Facets without releasing edges.
			// No edge should be orphan.
			oldFacets.ForEach(item => { Facets.Remove(item); });
			newFacets.ForEach(item => { Facets.Add(item); });

			return true;
		}

		void SetUpBounds()
		{
			//Utility.GetAllHalfEdges().ForEach(e => { GameObject.DestroyImmediate(e.gameObject); });
			Facets.ForEach(facet =>
			{
				Triangle.Release(facet, true);
			});

			Facets.Clear();

			superTriangle = Triangle.Create(new Vertex(stAPosition), new Vertex(stBPosition), new Vertex(stCPosition));

			Facets.Add(superTriangle);
		}

		void RemoveBounds()
		{
			Facets.RemoveAll(facet =>
			{
				if (facet.HasVertex(stAPosition) || facet.HasVertex(stBPosition) || facet.HasVertex(stCPosition))
				{
					Triangle.Release(facet, true);
					return true;
				}

				return false;
			});
		}

		Triangle FindFacetContainsVertex(out int hitEdgeIndex, Vertex vertex)
		{
			hitEdgeIndex = 0;
			findBoundTrianglePath.Clear();

			for (Triangle triangle = Facets[0]; triangle != null; )
			{
				findBoundTrianglePath.Add(triangle.Center);
				if (triangle.HasVertex(vertex))
				{
					return null;
				}

				int iedge = triangle.GetVertexDirection(vertex);
				if (iedge >= 0)
				{
					hitEdgeIndex = iedge;
					return triangle;
				}

				triangle = Utility.GetHalfEdgeByDirection(triangle, iedge).Pair.Face;
			}

			return null;
		}

		void InsertToFacet(Vertex v, Triangle old, List<Triangle> oldFacets, List<Triangle> newFacets)
		{
			Triangle ab = Triangle.Create(old);
			Triangle bc = Triangle.Create(old);
			Triangle ca = Triangle.Create(old);

			HalfEdge av = HalfEdge.Create(old.A, v);
			HalfEdge bv = HalfEdge.Create(old.B, v);
			HalfEdge cv = HalfEdge.Create(old.C, v);

			HalfEdge AB = old.AB, BC = old.BC, CA = old.CA;

			ab.Edge = Utility.CycleLink(AB, bv, av.Pair);
			ab.AllEdges.ForEach(item => { item.Face = ab; });
			//ab.Edge.Face = ab;

			bc.Edge = Utility.CycleLink(BC, cv, bv.Pair);
			//bc.Edge.Face = bc;
			bc.AllEdges.ForEach(item => { item.Face = bc; });

			ca.Edge = Utility.CycleLink(CA, av, cv.Pair);
			//ca.Edge.Face = ca;
			ca.AllEdges.ForEach(item => { item.Face = ca; });

			// Edges of old is in use.
			Triangle.Release(old);
			oldFacets.Add(old);

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

			newFacets.Add(ab);
			newFacets.Add(bc);
			newFacets.Add(ca);

			FlipIfNeeded(ab.Edge);
			FlipIfNeeded(bc.Edge);
			FlipIfNeeded(ca.Edge);
		}

		void InsertOnEdge(Vertex v, Triangle old, HalfEdge hitEdge, List<Triangle> oldFacets, List<Triangle> newFacets)
		{
			Triangle split1 = Triangle.Create(old);
			Triangle split2 = Triangle.Create(old);

			Vertex opositeVertex = hitEdge.Next.Dest;

			HalfEdge ov = HalfEdge.Create(opositeVertex, v);
			HalfEdge v1 = HalfEdge.Create(v, hitEdge.Dest);
			HalfEdge v2 = HalfEdge.Create(v, hitEdge.Pair.Dest);

			HalfEdge sp2Edge0 = hitEdge.Next.Next;
			HalfEdge sp2Edge1 = v2.Pair;
			HalfEdge sp2Edge2 = ov.Pair;
			split1.Edge = Utility.CycleLink(hitEdge.Next, ov, v1);
			split1.AllEdges.ForEach(item => { item.Face = split1; });

			split2.Edge = Utility.CycleLink(sp2Edge0, sp2Edge1, sp2Edge2);
			split2.AllEdges.ForEach(item => { item.Face = split2; });

			Triangle.Release(old);
			oldFacets.Add(old);

			Utility.Verify(ov.Face == split1);
			Utility.Verify(ov.Pair.Face == split2);
			//ov.Face = split1;
			//ov.Pair.Face = split2;

			newFacets.Add(split1);
			newFacets.Add(split2);

			Triangle other = hitEdge.Pair.Face;

			Triangle oposite1 = null;
			Triangle oposite2 = null;

			if (other != null)
			{
				Vertex p = hitEdge.Pair.Next.Dest;

				HalfEdge vp = HalfEdge.Create(v, p);

				oposite1 = Triangle.Create(other);
				oposite2 = Triangle.Create(other);

				HalfEdge op1Edge0 = hitEdge.Pair.Next.Next;
				HalfEdge op1Edge1 = v1.Pair;
				HalfEdge op1Edge2 = vp;

				oposite2.Edge = Utility.CycleLink(hitEdge.Pair.Next, vp.Pair, v2);
				oposite2.AllEdges.ForEach(item => { item.Face = oposite2; });

				oposite1.Edge = Utility.CycleLink(op1Edge0, op1Edge1, op1Edge2);
				oposite1.AllEdges.ForEach(item => { item.Face = oposite1; });

				Triangle.Release(other);
				oldFacets.Add(other);

				Utility.Verify(vp.Face == oposite1);
				Utility.Verify(vp.Pair.Face == oposite2);

				//vp.Face = oposite1;
				//vp.Pair.Face = oposite2;

				newFacets.Add(oposite1);
				newFacets.Add(oposite2);
			}

			Utility.Verify(v1.Face == split1);
			Utility.Verify(v1.Pair.Face == oposite1);

			//v1.Face = split1;
			//v1.Pair.Face = oposite1;

			Utility.Verify(v2.Face == oposite2);
			Utility.Verify(v2.Pair.Face == split2);

			//v2.Face = oposite2;
			//v2.Pair.Face = split2;

			HalfEdge.Release(hitEdge);

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

				a.AllEdges.ForEach(item => { item.Face = a; });
				b.AllEdges.ForEach(item => { item.Face = b; });

				if (halfEdge.Dest.Edge == halfEdge.Pair)
				{
					Utility.FixVertexHalfEdge(halfEdge.Dest);
				}

				if (halfEdge.Pair.Dest.Edge == halfEdge)
				{
					Utility.FixVertexHalfEdge(halfEdge.Pair.Dest);
				}

				if (stack.Count < EditorConstants.kMaxStackCapacity) stack.Push(halfEdge.Pair.Next.Next);

				if (stack.Count < EditorConstants.kMaxStackCapacity) stack.Push(halfEdge.Next);

				if (stack.Count < EditorConstants.kMaxStackCapacity) stack.Push(halfEdge.Pair.Next);

				if (stack.Count < EditorConstants.kMaxStackCapacity) stack.Push(halfEdge.Next.Next);

				HalfEdge.Release(halfEdge);
			}
		}
	}
}
