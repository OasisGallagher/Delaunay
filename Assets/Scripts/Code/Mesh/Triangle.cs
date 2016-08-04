using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Delaunay
{
	/// <summary>
	/// 三角形.
	/// </summary>
	public class Triangle : PathfindingNode
	{
		public static IDGenerator TriangleIDGenerator = new IDGenerator();

		public Triangle()
		{
			ID = TriangleIDGenerator.Value;
			Walkable = true;
		}

		public int ID { get; private set; }

		/// <summary>
		/// 三角形的一条包围边.
		/// </summary>
		public HalfEdge Edge
		{
			get { return halfEdge; }
			set
			{
				if (halfEdge == value) { return; }
				halfEdge = value;
				widthA = widthB = widthC = float.NaN;
			}
		}

		/// <summary>
		/// 三角形是否可行走.
		/// </summary>
		public bool Walkable { get; set; }

		/// <summary>
		/// 三角形的包围边.
		/// </summary>
		public List<HalfEdge> BoundingEdges
		{
			get
			{
				List<HalfEdge> answer = new List<HalfEdge>();
				if (AB == null) { return answer; }

				answer.Add(AB);

				// TODO: 判断 ???
				if (BC != AB) { answer.Add(BC); }
				if (CA != AB) { answer.Add(CA); }

				return answer;
			}
		}

		public HalfEdge AB { get { return Edge; } }
		public HalfEdge BC { get { return AB.Next; } }
		public HalfEdge CA { get { return AB.Next.Next; } }

		public HalfEdge BA { get { return AB.Pair; } }
		public HalfEdge CB { get { return BC.Pair; } }
		public HalfEdge AC { get { return CA.Pair; } }

		public Vertex A { get { return AB.Pair.Dest; } }
		public Vertex B { get { return AB.Dest; } }
		public Vertex C { get { return AB.Next.Dest; } }

		/// <summary>
		/// 获取可以经过a, b边的物体的最大半径(a, b必须有交点).
		/// </summary>
		public float GetWidth(HalfEdge a, HalfEdge b)
		{
			Vertex v = GetIntersectVertex(a, b);
			if (v == A)
			{
				if (float.IsNaN(widthA)) { widthA = CalculateWidth(a, b); }
				return widthA;
			}

			if (v == B)
			{
				if (float.IsNaN(widthB)) { widthB = CalculateWidth(a, b); }
				return widthB;
			}

			if (v == C)
			{
				if (float.IsNaN(widthC)) { widthC = CalculateWidth(a, b); }
				return widthC;
			}

			Utility.Verify(false, "Invalid edges {0} and {1}", a, b);
			return float.NaN;
		}

		/// <summary>
		/// 获取顶点from"正对"的边.
		/// </summary>
		public HalfEdge GetOpposite(Vertex from)
		{
			foreach (HalfEdge edge in BoundingEdges)
			{
				if (!from.Position.equals2(edge.Src.Position)
					&& !from.Position.equals2(edge.Dest.Position))
				{
					return edge.Pair;
				}
			}

			return null;
		}

		/// <summary>
		/// 获取边from"正对"的顶点.
		/// </summary>
		public Vertex GetOpposite(HalfEdge from)
		{
			if (AB.ID == from.ID) { return C; }
			if (BC.ID == from.ID) { return A; }
			if (CA.ID == from.ID) { return B; }
			Utility.Verify(false, "Invalid argument");
			return null;
		}

		/// <summary>
		/// 获取第i条边, [1, 3].
		/// </summary>
		public HalfEdge GetEdgeByIndex(int index)
		{
			index = Mathf.Abs(index);
			Utility.Verify(index >= 1 && index <= 3);
			return (index == 1) ? AB : (index == 2 ? BC : CA);
		}

		/// <summary>
		/// 点p是否在三角形内, 如果在边上, 返回onEdge的值.
		/// </summary>
		public bool Contains(Vector3 p, bool onEdge = true)
		{
			return MathUtility.PolygonContains(new Vector3[] { A.Position, B.Position, C.Position }, p, onEdge);
		}

		/// <summary>
		/// 获取点point所在的位置在三角形的方位.
		/// <para>返回:</para>
		/// <para>[-3, -1]: 在索引为(-i)的边上.</para>
		/// <para>0: 在三角形内.</para>
		/// <para>[1, 3]: 在索引为i的边的顺时针方向.</para>
		/// </summary>
		public int GetPointDirection(Vector3 point)
		{
			float t0 = point.cross2(B.Position, A.Position);
			
			if (MathUtility.Approximately(t0, 0)
				&& MathUtility.DiagonalRectContains(point, B.Position, A.Position))
			{
				return 1;
			}

			if (t0 > 0) { return -1; }

			float t1 = point.cross2(C.Position, B.Position);

			if (MathUtility.Approximately(t1, 0)
				&& MathUtility.DiagonalRectContains(point, C.Position, B.Position))
			{
				return 2;
			}

			if (t1 > 0) { return -2; }

			float t2 = point.cross2(A.Position, C.Position);

			if (MathUtility.Approximately(t2, 0)
				&& MathUtility.DiagonalRectContains(point, A.Position, C.Position))
			{
				return 3;
			}

			if (t2 > 0) { return -3; }

			return 0;
		}

		public override string ToString()
		{
			return "Triangle_" + ID + "_" + A.ToString() + " => " + B.ToString() + " => " + C.ToString();
		}

		/// <summary>
		/// 获取位置为position的顶点的索引. [0, 2].
		/// </summary>
		public int VertexIndex(Vector3 position)
		{
			if (A.Position.equals2(position)) { return 0; }
			if (B.Position.equals2(position)) { return 1; }
			if (C.Position.equals2(position)) { return 2; }

			return -1;
		}

		/// <summary>
		/// 三角形是否包含位置为position的顶点.
		/// </summary>
		public bool HasVertex(Vector3 position)
		{
			return VertexIndex(position) >= 0;
		}

		/// <summary>
		/// 获取ea和eb的交点.
		/// </summary>
		Vertex GetIntersectVertex(HalfEdge ea, HalfEdge eb)
		{
			if (ea.Src == eb.Dest) { return ea.Src; }
			if (ea.Dest == eb.Src) { return ea.Dest; }
			Utility.Verify(false, "Edge {0} and {1} has no intersection", ea, eb);
			return null;
		}

		/// <summary>
		/// 计算可以经过a, b边的物体的最大半径(a, b必须有交点).
		/// </summary>
		float CalculateWidth(HalfEdge ea, HalfEdge eb)
		{
			Vertex vc = GetIntersectVertex(ea, eb);

			Utility.Verify(vc != null);

			HalfEdge ec = GetOpposite(vc);
			Vertex va = GetOpposite(ea);
			Vertex vb = GetOpposite(eb);

			float d = (ea.Src.Position - ea.Dest.Position).magnitude2();
			d = Mathf.Min(d, (eb.Src.Position - eb.Dest.Position).magnitude2());

			if (vc.Position.dot2(vb.Position, va.Position) <= 0
				|| vc.Position.dot2(va.Position, vb.Position) <= 0)
			{
				return d;
			}

			if (ec.Constrained)
			{
				return MathUtility.MinDistance2Segment(vc.Position, ec.Src.Position, ec.Dest.Position);
			}

			return SearchWidth(vc, this, ec, d);
		}

		float SearchWidth(Vertex c, Triangle t, HalfEdge e, float d)
		{
			Vertex u = e.Src, v = e.Dest;
			if (c.Position.dot2(v.Position, u.Position) <= 0
				|| c.Position.dot2(u.Position, v.Position) <= 0)
			{
				return d;
			}

			float d2 = MathUtility.MinDistance2Segment(c.Position, e.Src.Position, e.Dest.Position);
			if (d2 > d)
			{
				return d;
			}

			if (e.Constrained)
			{
				return d2;
			}

			Triangle t2 = e.Pair.Face;

			if (t2 == null)
			{
				Debug.LogError("Invalid t2");
				return d;
			}

			HalfEdge e2 = t2.GetOpposite(e.Src);
			HalfEdge e3 = t2.GetOpposite(e.Dest);

			d = SearchWidth(c, t2, e2, d);
			return SearchWidth(c, t2, e3, d);
		}

		/// <summary>
		/// 序列化三角形.
		/// </summary>
		public void WriteBinary(BinaryWriter writer)
		{
			writer.Write(ID);
			writer.Write(Edge != null ? Edge.ID : -1);
			writer.Write(Walkable);
		}

		/// <summary>
		/// 反序列化三角形.
		/// </summary>
		public void ReadBinary(BinaryReader reader, IDictionary<int, HalfEdge> container)
		{
			ID = reader.ReadInt32();

			int edge = reader.ReadInt32();
			Edge = container[edge];
			BoundingEdges.ForEach(e => { e.Face = this; });

			Walkable = reader.ReadBoolean();
		}

		#region PathfindingNode

		/// <summary>
		/// 获取该三角形的包围边满足:
		/// <para>1. 该边和其Pair都是非约束的.</para>
		/// <para>2. 该边对面是可行走三角形.</para>
		/// </summary>
		public override HalfEdge[] AdjacencyPortals
		{
			get { return GetAdjacencyPortals(); }
		}

		#endregion

		HalfEdge[] GetAdjacencyPortals()
		{
			List<HalfEdge> answer = new List<HalfEdge>(3);
			foreach (HalfEdge edge in BoundingEdges)
			{
				if (edge.Constrained || edge.Pair.Constrained) { continue; }

				if (edge.Pair.Face != null && edge.Pair.Face.Walkable)
				{
					answer.Add(edge.Pair);
				}
			}

			return answer.ToArray();
		}

		HalfEdge halfEdge = null;

		float widthA = float.NaN;
		float widthB = float.NaN;
		float widthC = float.NaN;
	}
}
