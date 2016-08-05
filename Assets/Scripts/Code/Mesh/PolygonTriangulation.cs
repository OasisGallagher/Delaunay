using System.Collections.Generic;
using UnityEngine;

namespace Delaunay
{
	public static class PolygonTriangulation
	{
		/// <summary>
		/// ����polygon�ڵĶ�����ɵĶ���������λ�.
		/// <para>ans[3*i], ans[3*i+1], ans[3*i+2]Ϊ���ɵĵ�i��������.</para>
		/// </summary>
		public static List<Vertex> Triangulate(List<Vertex> polygon)
		{
			return EarClipping.Triangulate(polygon);
		}

		/// <summary>
		/// ����polygon�ڵĶ�����ɵĶ���������λ�, �ұ�֤constrainedEdgeSrc����һ����constrainedEdgeDest�ı�.
		/// <para>ans[3*i], ans[3*i+1], ans[3*i+2]Ϊ���ɵĵ�i��������.</para>
		/// </summary>
		public static List<Vertex> Triangulate(List<Vertex> polygon, Vertex constrainedEdgeSrc, Vertex constrainedEdgeDest)
		{
			return TriangulatePolygonDelaunay(polygon, constrainedEdgeSrc, constrainedEdgeDest);
		}

		public static class EarClipping
		{
			/// <summary>
			/// �ö��з�, ����polygon��ʾ�Ķ���������λ�.
			/// <para>ans[3*i], ans[3*i+1], ans[3*i+2]Ϊ���ɵĵ�i��������.</para>
			/// </summary>
			public static List<Vertex> Triangulate(List<Vertex> polygon)
			{
				// ��������, ��������ɾ��.
				ArrayLinkedList<EarVertex> vertices = new ArrayLinkedList<EarVertex>(polygon.Count);
				polygon.ForEach(item => { vertices.Add(new EarVertex() { vertex = item }); });

				// �ռ���ǰ���еĶ��������.
				ArrayLinkedList<int> earTips = new ArrayLinkedList<int>(polygon.Count);

				for (int index = 0; index < polygon.Count; ++index)
				{
					// �����indexΪ�����Ķ�����ڽ��Ƿ�Ϊ�Ž�.
					if (CheckIsReflex(vertices, index))
					{
						vertices[index].SetMask((int)EarVertex.Mask.IsReflex, true);
					}
					// ����ýǲ�Ϊ�Ž�, ������Ƿ�Ϊ����(�Žǲ�����Ϊ����).
					else if (CheckIsEar(vertices, index))
					{
						vertices[index].SetMask((int)EarVertex.Mask.IsEar, true);
						vertices[index].earListIndex = earTips.Add(index);
					}
				}

				return DoTriangulate(vertices, earTips);
			}

			/// <summary>
			/// ���vertices������Ϊcurrent�Ķ����Ƿ�Ϊ����.
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

				// ���㹲��, ��Ȼ���Ƕ���.
				if (MathUtility.Approximately(points[0].cross2(points[2], points[1]), 0f))
				{
					return false;
				}

				// ���prev, current, next��ɵ�������, �Ƿ����������.
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

				// ���������������, ��ô�õ�Ϊ����.
				return true;
			}

			/// <summary>
			/// ���vertices[index]���ڽ�, �Ƿ�Ϊ�Ž�.
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
			/// ��vertices��ɵĶ���������λ�.
			/// <para>earTipsΪ��ǰ�Ķ��������.</para>
			static List<Vertex> DoTriangulate(ArrayLinkedList<EarVertex> vertices, ArrayLinkedList<int> earTips)
			{
				// N����, �γ�N-2��������, ��3*(N-2)������.
				List<Vertex> answer = new List<Vertex>((vertices.Count - 2) * 3);

				// ���Ƴ��Ķ���.
				EarVertex[] removedEars = new EarVertex[2];
				int removedEarCount = 0;

				// ��Ҫ���Ƴ��Ķ������earTips�е�����.
				int earTipIndex = -1;
				for (var e = earTips.GetEnumerator(); e.MoveNext(); )
				{
					if (earTipIndex >= 0) { earTips.RemoveAt(earTipIndex); }

					earTipIndex = e.CurrentIndex;

					// ��Ҫ�Ƴ��Ķ������vertices�е�����.
					int earTipVertexIndex = earTips[earTipIndex];
					// ��Ҫ�Ƴ��Ķ���ڵ�.
					EarVertex earTipVertex = vertices[earTipVertexIndex];

					// ����ڵ����һ���ڵ�.
					int prevIndex = vertices.PrevIndex(earTipVertexIndex);
					EarVertex prevVertex = vertices.PrevValue(earTipVertexIndex);

					// ����ڵ����һ���ڵ�.
					int nextIndex = vertices.NextIndex(earTipVertexIndex);
					EarVertex nextVertex = vertices.NextValue(earTipVertexIndex);

					// �����µ�������.
					answer.Add(prevVertex.vertex);
					answer.Add(earTipVertex.vertex);
					answer.Add(nextVertex.vertex);

					// �Ըýڵ�Ϊ����Ķ����ѱ�"�е�", �Ƴ�����ڵ�.
					vertices.RemoveAt(earTipVertexIndex);

					// ���¸ýڵ���Ͻڵ��״̬.
					int state = UpdateEarVertexState(vertices, prevIndex);
					// �����µĶ���.
					if (state > 0)
					{
						prevVertex.earListIndex = earTips.Add(prevIndex);
					}
					// �ռ�֮ǰ��, �����ڲ����Ƕ���Ľڵ�.
					else if (state < 0)
					{
						removedEars[removedEarCount++] = prevVertex;
					}

					// ���¸ýڵ���½ڵ��״̬.
					state = UpdateEarVertexState(vertices, nextIndex);
					if (state > 0)
					{
						nextVertex.earListIndex = earTips.Add(nextIndex);
					}
					else if (state < 0)
					{
						removedEars[removedEarCount++] = nextVertex;
					}

					// ��earTips���Ƴ�֮ǰ��, ���ڲ��Ƕ���Ľڵ�.
					for (int i = 0; i < removedEarCount; ++i)
					{
						Utility.Verify(removedEars[i].earListIndex >= 0);
						earTips.RemoveAt(removedEars[i].earListIndex);
						removedEars[i].earListIndex = -1;
					}

					removedEarCount = 0;
				}

				// �Ƴ����һ������, ���earTips.
				if (earTipIndex >= 0) { earTips.RemoveAt(earTipIndex); }

				return answer;
			}

			/// <summary>
			/// ����vertices[vertexIndex]��״̬.
			/// <para>����:</para>
			/// <para>-1: (�ö���֮ǰ�Ƕ���, ���ڲ���).</para>
			/// <para> 0: (����״̬û�����仯).</para>
			/// <para> 1: (�ö���֮ǰ���Ƕ���, ������).</para>
			/// </summary>
			static int UpdateEarVertexState(ArrayLinkedList<EarVertex> vertices, int vertexIndex)
			{
				EarVertex earVertex = vertices[vertexIndex];

				int result = 0;

				bool isEar = earVertex.TestMask((int)EarVertex.Mask.IsEar);

				// ����ö�����ڽ����Ž�, ��ô��֮ǰҲ��Ȼ���Ƕ���.
				if (earVertex.TestMask((int)EarVertex.Mask.IsReflex))
				{
					Utility.Verify(!isEar);

					// ����ö�����ڽǲ������Ž�, ���Ѿ���Ϊ���µĶ���.
					// ����ö�����ڽ���Ȼ���ŽǵĻ�, ��ô������㲻���ܳ�Ϊ����, 
					// �����ڽ��ж����ж�ǰ, ���Ƚ���Ч�ʸ��ߵ��ڽ��ж�.
					if (!earVertex.SetMask((int)EarVertex.Mask.IsReflex, CheckIsReflex(vertices, vertexIndex))
						&& earVertex.SetMask((int)EarVertex.Mask.IsEar, CheckIsEar(vertices, vertexIndex)))
					{
						result = 1;
					}
				}
				// ���֮ǰΪ�������ڲ��Ƕ���(��֮��Ȼ).
				else if (isEar != earVertex.SetMask((int)EarVertex.Mask.IsEar, CheckIsEar(vertices, vertexIndex)))
				{
					result = 1;
					// ֮ǰ�Ƕ���, ���ڲ��Ƕ���.
					if (isEar) { result = -result; }
				}

				return result;
			}

			class EarVertex : Maskable
			{
				public Vertex vertex;

				/// <summary>
				/// �����ǰ����Ϊ����, ��¼�ýڵ��ڶ�����е�����.
				/// <para>Ϊ��ʹ�ڶ�������Ƴ��ýڵ�ʱ, �ﵽO(1)�ĸ��Ӷ�.</para>
				/// </summary>
				public int earListIndex = -1;

				public enum Mask
				{
					/// <summary>
					/// �ö�����ڽ��Ƿ�Ϊ�Ž�.
					/// </summary>
					IsReflex = 1,

					/// <summary>
					/// �ö����Ƿ�Ϊ����.
					/// </summary>
					IsEar = 2,
				}
			}
		}

		/// <summary>
		/// ����polygon�ڵĶ�����ɵĶ���������λ�, �ұ�֤src����һ����dest�ı�.
		/// <para>ans[3*i], ans[3*i+1], ans[3*i+2]Ϊ���ɵĵ�i��������.</para>
		/// </summary>
		static List<Vertex> TriangulatePolygonDelaunay(List<Vertex> polygon, Vertex src, Vertex dest)
		{
			List<Vertex> answer = new List<Vertex>();

			if (polygon.Count == 0) { return answer; }

			Vertex c = polygon[0];
			if (polygon.Count > 1)
			{
				// Ѱ��һ������c, ʹ��src, dest, c��ɵ������ε����Բ��, �����������Ķ���.
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

				// �Ե�cΪ��, ��polygon�Ķ����Ϊ����������.
				foreach (Vertex v in polygon)
				{
					if (v == c)
					{
						current = right;
						continue;
					}

					current.Add(v);
				}

				// �����������ֵݹ���ø÷���.
				answer.AddRange(TriangulatePolygonDelaunay(left, src, c));
				answer.AddRange(TriangulatePolygonDelaunay(right, c, dest));
			}

			// ������ɵ�������.
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
