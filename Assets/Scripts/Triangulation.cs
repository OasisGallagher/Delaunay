using System.Collections.Generic;
using UnityEngine;

namespace Delaunay
{
	public static class TriangulationTools
	{
		public static List<Vertex> Triangulate(List<Vertex> polygon)
		{
			return EarClipping(polygon);
		}

		public static List<Vertex> Triangulate(List<Vertex> polygon, Vertex constraintSrc, Vertex constraintDest)
		{
			return TriangulatePolygonDelaunay(polygon, constraintSrc, constraintDest);
		}

		#region Ear clipping
		class EarVertex
		{
			public Vertex vertex;
			public int earListIndex = -1;
			public int mask = 0;

			public enum Mask
			{
				IsReflex = 1,
				IsEar = 2,
			}

			public bool SetMask(Mask value, bool addMask)
			{
				if (addMask)
				{
					mask |= (int)value;
				}
				else
				{
					mask &= (int)(~value);
				}

				return addMask;
			}

			public bool TestMask(Mask value)
			{
				return (mask & (int)value) != 0;
			}

			public override string ToString()
			{
				return vertex.ID + "&" + mask;
			}
		}

		static List<Vertex> EarClipping(List<Vertex> polygon)
		{
			ArrayLinkedList<EarVertex> vertices = new ArrayLinkedList<EarVertex>(polygon.Count + 2);
			polygon.ForEach(item => { vertices.Add(new EarVertex() { vertex = item, mask = 0 }); });

			ArrayLinkedList<int> earTips = new ArrayLinkedList<int>(polygon.Count);

			for (int index = 0; index < polygon.Count; ++index)
			{
				if (ChkIsReflex(vertices, index))
				{
					vertices[index].SetMask(EarVertex.Mask.IsReflex, true);
				}
				else if (ChkIsEar(vertices, index))
				{
					vertices[index].SetMask(EarVertex.Mask.IsEar, true);
					vertices[index].earListIndex = earTips.Add(index);
				}
			}

			return EarClipping(vertices, earTips);
		}

		static bool ChkIsEar(ArrayLinkedList<EarVertex> vertices, int current)
		{
			if (vertices.Count < 3) { return false; }

			int prev = vertices.PrevIndex(current);
			int next = vertices.NextIndex(current);

			Vector3[] points = new Vector3[]
			{
				vertices[prev].vertex.Position, 
				vertices[current].vertex.Position,
				vertices[next].vertex.Position 
			};

			if (Mathf.Approximately(points[0].cross2(points[2], points[1]), 0f))
			{
				return false;
			}

			for (var e = vertices.GetIndexEnumerator(); e.MoveNext(); )
			{
				if (e.ListIndex == current || e.ListIndex == prev || e.ListIndex == next)
				{
					continue;
				}

				if (MathUtility.PolygonContains(points, vertices[e.ListIndex].vertex.Position))
				{
					return false;
				}
			}

			return true;
		}

		static bool ChkIsReflex(ArrayLinkedList<EarVertex> vertices, int index)
		{
			Vertex current = vertices[index].vertex;
			Vertex prev = vertices.PrevValue(index).vertex;
			Vertex next = vertices.NextValue(index).vertex;
			return next.Position.cross2(prev.Position, current.Position) < 0f;
		}

		static List<Vertex> EarClipping(ArrayLinkedList<EarVertex> vertices, ArrayLinkedList<int> earTips)
		{
			List<Vertex> answer = new List<Vertex>(vertices.Count - 2);
			List<EarVertex> removedEars = new List<EarVertex>(vertices.Count);

			int earTipIndex = -1;
			for (var e = earTips.GetIndexEnumerator(); e.MoveNext(); )
			{
				if (earTipIndex >= 0) { earTips.RemoveAt(earTipIndex); }

				earTipIndex = e.ListIndex;

				int earTipVertexIndex = earTips[earTipIndex];
				EarVertex earTipVertex = vertices[earTipVertexIndex];

				int prevIndex = vertices.PrevIndex(earTipVertexIndex);
				EarVertex prevVertex = vertices.PrevValue(earTipVertexIndex);

				int nextIndex = vertices.NextIndex(earTipVertexIndex);
				EarVertex nextVertex = vertices.NextValue(earTipVertexIndex);

				answer.Add(prevVertex.vertex);
				answer.Add(earTipVertex.vertex);
				answer.Add(nextVertex.vertex);

				vertices.RemoveAt(earTipVertexIndex);

				int state = UpdateEarVertexState(vertices, prevIndex);
				if (state > 0)
				{
					prevVertex.earListIndex = earTips.Add(prevIndex);
				}
				else if (state < 0)
				{
					removedEars.Add(prevVertex);
				}

				state = UpdateEarVertexState(vertices, nextIndex);
				if (state > 0)
				{
					nextVertex.earListIndex = earTips.Add(nextIndex);
				}
				else if (state < 0)
				{
					removedEars.Add(nextVertex);
				}

				removedEars.ForEach(item =>
				{
					Utility.Verify(item.earListIndex >= 0);
					earTips.RemoveAt(item.earListIndex);
					item.earListIndex = -1;
				});

				removedEars.Clear();
			}

			if (earTipIndex >= 0) { earTips.RemoveAt(earTipIndex); }

			return answer;
		}

		static int UpdateEarVertexState(ArrayLinkedList<EarVertex> vertices, int vertexIndex)
		{
			EarVertex earVertex = vertices[vertexIndex];

			int result = 0;

			bool isEar = earVertex.TestMask(EarVertex.Mask.IsEar);
			if (earVertex.TestMask(EarVertex.Mask.IsReflex))
			{
				Utility.Verify(!isEar);
				if (!earVertex.SetMask(EarVertex.Mask.IsReflex, ChkIsReflex(vertices, vertexIndex))
					&& earVertex.SetMask(EarVertex.Mask.IsEar, ChkIsEar(vertices, vertexIndex)))
				{
					result = 1;
				}
			}
			else if (isEar != earVertex.SetMask(EarVertex.Mask.IsEar, ChkIsEar(vertices, vertexIndex)))
			{
				result = 1;
				if (isEar) { result = -result; }
			}

			return result;
		}

		#endregion

		static List<Vertex> TriangulatePolygonDelaunay(List<Vertex> polygon, Vertex src, Vertex dest)
		{
			List<Vertex> answer = new List<Vertex>();

			if (polygon.Count == 0) { return answer; }

			Vertex c = polygon[0];
			if (polygon.Count > 1)
			{
				foreach (Vertex v in polygon)
				{
					if (MathUtility.PointInCircumCircle(src, dest, c, v))
					{
						c = v;
					}
				}

				List<Vertex> left = new List<Vertex>();
				List<Vertex> right = new List<Vertex>();
				List<Vertex> current = left;

				foreach (Vertex v in polygon)
				{
					if (v == c)
					{
						current = right;
						continue;
					}

					current.Add(v);
				}

				answer.AddRange(TriangulatePolygonDelaunay(left, src, c));
				answer.AddRange(TriangulatePolygonDelaunay(right, c, dest));
			}

			if (polygon.Count > 0)
			{
				answer.Add(src);
				answer.Add(dest);
				answer.Add(c);
			}

			return answer;
		}
	}
}
