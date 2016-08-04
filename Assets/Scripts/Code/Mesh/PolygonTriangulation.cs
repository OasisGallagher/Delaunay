using System.Collections.Generic;
using UnityEngine;

namespace Delaunay
{
	public static class PolygonTriangulation
	{
		/// <summary>
		/// 将由polygon内的顶点组成的多边形三角形化.
		/// <para>ans[3*i], ans[3*i+1], ans[3*i+2]为生成的第i个三角形.</para>
		/// </summary>
		public static List<Vertex> Triangulate(List<Vertex> polygon)
		{
			return EarClipping.Triangulate(polygon);
		}

		/// <summary>
		/// 将由polygon内的顶点组成的多边形三角形化, 且保证constrainedEdgeSrc存在一条到constrainedEdgeDest的边.
		/// <para>ans[3*i], ans[3*i+1], ans[3*i+2]为生成的第i个三角形.</para>
		/// </summary>
		public static List<Vertex> Triangulate(List<Vertex> polygon, Vertex constrainedEdgeSrc, Vertex constrainedEdgeDest)
		{
			return TriangulatePolygonDelaunay(polygon, constrainedEdgeSrc, constrainedEdgeDest);
		}

		public static class EarClipping
		{
			/// <summary>
			/// 用耳切法, 将由polygon表示的多边形三角形化.
			/// <para>ans[3*i], ans[3*i+1], ans[3*i+2]为生成的第i个三角形.</para>
			/// </summary>
			public static List<Vertex> Triangulate(List<Vertex> polygon)
			{
				// 构造链表, 方便插入和删除.
				ArrayLinkedList<EarVertex> vertices = new ArrayLinkedList<EarVertex>(polygon.Count);
				polygon.ForEach(item => { vertices.Add(new EarVertex() { vertex = item }); });

				// 收集当前所有的耳朵的索引.
				ArrayLinkedList<int> earTips = new ArrayLinkedList<int>(polygon.Count);

				for (int index = 0; index < polygon.Count; ++index)
				{
					// 检查以index为索引的顶点的内角是否为优角.
					if (CheckIsReflex(vertices, index))
					{
						vertices[index].SetMask((int)EarVertex.Mask.IsReflex, true);
					}
					// 如果该角不为优角, 检查它是否为耳朵(优角不可能为耳朵).
					else if (CheckIsEar(vertices, index))
					{
						vertices[index].SetMask((int)EarVertex.Mask.IsEar, true);
						vertices[index].earListIndex = earTips.Add(index);
					}
				}

				return DoTriangulate(vertices, earTips);
			}

			/// <summary>
			/// 检查vertices中索引为current的顶点是否为耳朵.
			/// </summary>
			static bool CheckIsEar(ArrayLinkedList<EarVertex> vertices, int current)
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

				// 三点共线, 必然不是耳朵.
				if (MathUtility.Approximately(points[0].cross2(points[2], points[1]), 0f))
				{
					return false;
				}

				// 检查prev, current, next组成的三角形, 是否包含其它点.
				for (var e = vertices.GetEnumerator(); e.MoveNext(); )
				{
					if (e.CurrentIndex == current || e.CurrentIndex == prev || e.CurrentIndex == next)
					{
						continue;
					}

					if (MathUtility.PolygonContains(points, vertices[e.CurrentIndex].vertex.Position))
					{
						return false;
					}
				}

				// 如果不包含其它点, 那么该点为耳朵.
				return true;
			}

			/// <summary>
			/// 检查vertices[index]的内角, 是否为优角.
			/// </summary>
			/// <param name="vertices"></param>
			/// <param name="index"></param>
			/// <returns></returns>
			static bool CheckIsReflex(ArrayLinkedList<EarVertex> vertices, int index)
			{
				Vertex current = vertices[index].vertex;
				Vertex prev = vertices.PrevValue(index).vertex;
				Vertex next = vertices.NextValue(index).vertex;
				return next.Position.cross2(prev.Position, current.Position) < 0f;
			}

			/// <summary>
			/// 对vertices组成的多边形三角形化.
			/// <para>earTips为当前的耳朵的索引.</para>
			static List<Vertex> DoTriangulate(ArrayLinkedList<EarVertex> vertices, ArrayLinkedList<int> earTips)
			{
				// N边行, 形成N-2个三角形, 共3*(N-2)个顶点.
				List<Vertex> answer = new List<Vertex>((vertices.Count - 2) * 3);

				// 被移除的耳朵.
				EarVertex[] removedEars = new EarVertex[2];
				int removedEarCount = 0;

				// 需要被移除的耳朵的在earTips中的索引.
				int earTipIndex = -1;
				for (var e = earTips.GetEnumerator(); e.MoveNext(); )
				{
					if (earTipIndex >= 0) { earTips.RemoveAt(earTipIndex); }

					earTipIndex = e.CurrentIndex;

					// 需要移除的耳朵的在vertices中的索引.
					int earTipVertexIndex = earTips[earTipIndex];
					// 需要移除的耳朵节点.
					EarVertex earTipVertex = vertices[earTipVertexIndex];

					// 耳朵节点的上一个节点.
					int prevIndex = vertices.PrevIndex(earTipVertexIndex);
					EarVertex prevVertex = vertices.PrevValue(earTipVertexIndex);

					// 耳朵节点的下一个节点.
					int nextIndex = vertices.NextIndex(earTipVertexIndex);
					EarVertex nextVertex = vertices.NextValue(earTipVertexIndex);

					// 构成新的三角形.
					answer.Add(prevVertex.vertex);
					answer.Add(earTipVertex.vertex);
					answer.Add(nextVertex.vertex);

					// 以该节点为耳尖的耳朵已被"切掉", 移除这个节点.
					vertices.RemoveAt(earTipVertexIndex);

					// 更新该节点的上节点的状态.
					int state = UpdateEarVertexState(vertices, prevIndex);
					// 加入新的耳朵.
					if (state > 0)
					{
						prevVertex.earListIndex = earTips.Add(prevIndex);
					}
					// 收集之前是, 而现在不再是耳朵的节点.
					else if (state < 0)
					{
						removedEars[removedEarCount++] = prevVertex;
					}

					// 更新该节点的下节点的状态.
					state = UpdateEarVertexState(vertices, nextIndex);
					if (state > 0)
					{
						nextVertex.earListIndex = earTips.Add(nextIndex);
					}
					else if (state < 0)
					{
						removedEars[removedEarCount++] = nextVertex;
					}

					// 在earTips中移除之前是, 现在不是耳朵的节点.
					for (int i = 0; i < removedEarCount; ++i)
					{
						Utility.Verify(removedEars[i].earListIndex >= 0);
						earTips.RemoveAt(removedEars[i].earListIndex);
						removedEars[i].earListIndex = -1;
					}

					removedEarCount = 0;
				}

				// 移除最后一个耳朵, 清空earTips.
				if (earTipIndex >= 0) { earTips.RemoveAt(earTipIndex); }

				return answer;
			}

			/// <summary>
			/// 更新vertices[vertexIndex]的状态.
			/// <para>返回:</para>
			/// <para>-1: (该顶点之前是耳朵, 现在不是).</para>
			/// <para> 0: (耳朵状态没发生变化).</para>
			/// <para> 1: (该顶点之前不是耳朵, 现在是).</para>
			/// </summary>
			static int UpdateEarVertexState(ArrayLinkedList<EarVertex> vertices, int vertexIndex)
			{
				EarVertex earVertex = vertices[vertexIndex];

				int result = 0;

				bool isEar = earVertex.TestMask((int)EarVertex.Mask.IsEar);

				// 如果该顶点的内角是优角, 那么它之前也不然不是耳朵.
				if (earVertex.TestMask((int)EarVertex.Mask.IsReflex))
				{
					Utility.Verify(!isEar);

					// 如果该顶点的内角不再是优角, 且已经成为了新的耳朵.
					// 如果该顶点的内角依然是优角的话, 那么这个顶点不可能成为耳朵, 
					// 所以在进行耳朵判断前, 优先进行效率更高的内角判断.
					if (!earVertex.SetMask((int)EarVertex.Mask.IsReflex, CheckIsReflex(vertices, vertexIndex))
						&& earVertex.SetMask((int)EarVertex.Mask.IsEar, CheckIsEar(vertices, vertexIndex)))
					{
						result = 1;
					}
				}
				// 如果之前为耳朵现在不是耳朵(反之亦然).
				else if (isEar != earVertex.SetMask((int)EarVertex.Mask.IsEar, CheckIsEar(vertices, vertexIndex)))
				{
					result = 1;
					// 之前是耳朵, 现在不是耳朵.
					if (isEar) { result = -result; }
				}

				return result;
			}

			class EarVertex : Maskable
			{
				public Vertex vertex;

				/// <summary>
				/// 如果当前顶点为耳朵, 记录该节点在耳朵表中的索引.
				/// <para>为了使在耳朵表中移除该节点时, 达到O(1)的复杂度.</para>
				/// </summary>
				public int earListIndex = -1;

				public enum Mask
				{
					/// <summary>
					/// 该顶点的内角是否为优角.
					/// </summary>
					IsReflex = 1,

					/// <summary>
					/// 该顶点是否为耳朵.
					/// </summary>
					IsEar = 2,
				}
			}
		}

		/// <summary>
		/// 将由polygon内的顶点组成的多边形三角形化, 且保证src存在一条到dest的边.
		/// <para>ans[3*i], ans[3*i+1], ans[3*i+2]为生成的第i个三角形.</para>
		/// </summary>
		static List<Vertex> TriangulatePolygonDelaunay(List<Vertex> polygon, Vertex src, Vertex dest)
		{
			List<Vertex> answer = new List<Vertex>();

			if (polygon.Count == 0) { return answer; }

			Vertex c = polygon[0];
			if (polygon.Count > 1)
			{
				// 寻找一个顶点c, 使得src, dest, c组成的三角形的外接圆中, 不包含其它的顶点.
				foreach (Vertex v in polygon)
				{
					if (MathUtility.PointInCircumCircle(src.Position, dest.Position, c.Position, v.Position))
					{
						c = v;
					}
				}

				List<Vertex> left = new List<Vertex>();
				List<Vertex> right = new List<Vertex>();
				List<Vertex> current = left;

				// 以点c为界, 将polygon的顶点分为左右两部分.
				foreach (Vertex v in polygon)
				{
					if (v == c)
					{
						current = right;
						continue;
					}

					current.Add(v);
				}

				// 对左右两部分递归调用该方法.
				answer.AddRange(TriangulatePolygonDelaunay(left, src, c));
				answer.AddRange(TriangulatePolygonDelaunay(right, c, dest));
			}

			// 添加生成的三角形.
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
