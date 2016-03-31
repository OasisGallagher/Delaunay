using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Delaunay
{
	public class Triangle : MonoBehaviour
	{
		public int ID { get; private set; }

		public static IDGenerator TriangleIDGenerator = new IDGenerator();

		public void _Awake()
		{
			ID = TriangleIDGenerator.Value;
			Walkable = true;
			gameObject.name = "Triangle_" + ID;
		}

		void OnDrawGizmosSelected()
		{
			Color oldColor = Gizmos.color;

			Gizmos.color = Color.red;
			Gizmos.DrawSphere(A.Position + Vector3.up * 0.6f, 0.3f);

			Gizmos.color = Color.green;
			Gizmos.DrawSphere(B.Position + Vector3.up * 0.6f, 0.3f);

			Gizmos.color = Color.blue;
			Gizmos.DrawSphere(C.Position + Vector3.up * 0.6f, 0.3f);

			Gizmos.color = oldColor;
		}

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

		public bool Place(out Vector3 center, Vector3 reference, float radius)
		{
			return MathUtility.Place(out center, A.Position, B.Position, C.Position, reference, radius);
		}

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

		public Vertex GetOpposite(HalfEdge from)
		{
			if (AB.ID == from.ID) { return C; }
			if (BC.ID == from.ID) { return A; }
			if (CA.ID == from.ID) { return B; }
			Utility.Verify(false, "Invalid argument");
			return null;
		}

		public HalfEdge GetEdgeByDirection(int direction)
		{
			direction = Mathf.Abs(direction);
			Utility.Verify(direction >= 1 && direction <= 3);
			return (direction == 1) ? AB : (direction == 2 ? BC : CA);
		}

		/// <summary>
		/// One of the half-edges bordering the face.
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

		public bool Walkable { get; set; }

		public List<HalfEdge> BoundingEdges
		{
			get
			{
				List<HalfEdge> answer = new List<HalfEdge>();
				if (AB == null) { return answer; }

				answer.Add(AB);
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

		public bool Contains(Vertex t, bool onEdge = true)
		{
			return Contains(t.Position);
		}

		public bool Contains(Vector3 p, bool onEdge = true)
		{
			return MathUtility.PolygonContains(new Vector3[] { A.Position, B.Position, C.Position }, p, onEdge);
		}

		public Vertex FindVertex(Vector3 point)
		{
			if (A.Position.equals2(point)) { return A; }
			if (B.Position.equals2(point)) { return B; }
			if (C.Position.equals2(point)) { return C; }
			return null;
		}

		public int GetPointDirection(Vector3 point)
		{
			float t0 = point.cross2(B.Position, A.Position);
			if (t0 > 0) { return -1; }

			if (Mathf.Approximately(t0, 0)
				&& MathUtility.DiagonalRectContains(point, B.Position, A.Position))
			{
				return 1;
			}

			float t1 = point.cross2(C.Position, B.Position);
			if (t1 > 0) { return -2; }

			if (Mathf.Approximately(t1, 0)
				&& MathUtility.DiagonalRectContains(point, C.Position, B.Position))
			{
				return 2;
			}

			float t2 = point.cross2(A.Position, C.Position);
			if (t2 > 0) { return -3; }

			if (Mathf.Approximately(t2, 0)
				&& MathUtility.DiagonalRectContains(point, A.Position, C.Position))
			{
				return 3;
			}

			return 0;
		}

		public override string ToString()
		{
			return gameObject.name + "_" + A.ToString() + " => " + B.ToString() + " => " + C.ToString();
		}

		public bool HasVertex(Vertex v)
		{
			return A.equals2(v) || B.equals2(v) || C.equals2(v);
		}

		public bool HasVertex(Vector3 position)
		{
			return A.Position.equals2(position)
				|| B.Position.equals2(position) 
				|| C.Position.equals2(position);
		}

		void UpdateWidth()
		{
			widthA = CalculateWidth(AB, CA);
			widthB = CalculateWidth(BC, AB);
			widthC = CalculateWidth(CA, BC);
		}

		Vertex GetIntersectVertex(HalfEdge ea, HalfEdge eb)
		{
			if (ea.Src == eb.Dest) { return ea.Src; }
			if (ea.Dest == eb.Src) { return ea.Dest; }
			Utility.Verify(false, "Edge {0} and {1} has no intersection", ea, eb);
			return null;
		}

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

			if (ec.Constraint)
			{
				return MathUtility.MinDistance(vc.Position, ec.Src.Position, ec.Dest.Position);
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

			float d2 = MathUtility.MinDistance(c.Position, e.Src.Position, e.Dest.Position);
			if (d2 > d)
			{
				return d;
			}

			if (e.Constraint)
			{
				return d2;
			}

			Triangle t2 = e.Pair.Face;

			if (t2 == null)
			{
				print("Invalid t2");
				return d;
			}

			HalfEdge e2 = t2.GetOpposite(e.Src);
			HalfEdge e3 = t2.GetOpposite(e.Dest);

			d = SearchWidth(c, t2, e2, d);
			return SearchWidth(c, t2, e3, d);
		}

		public void WriteBinary(BinaryWriter writer)
		{
			writer.Write(ID);
			writer.Write(Edge != null ? Edge.ID : -1);
			writer.Write(Walkable);
		}

		public void ReadBinary(BinaryReader reader, IDictionary<int, HalfEdge> container)
		{
			ID = reader.ReadInt32();

			int edge = reader.ReadInt32();
			Edge = container[edge];
			BoundingEdges.ForEach(e => { e.Face = this; });

			Walkable = reader.ReadBoolean();
		}

		#region Pathfinding
		public HalfEdge Portal { get; set; }

		public HalfEdge[] AdjPortals { get { return GetAdjPortals(); } }

		public float G { get; set; }

		public float H { get; set; }

		public uint Flag { get; set; }
		#endregion

		HalfEdge[] GetAdjPortals()
		{
			List<HalfEdge> answer = new List<HalfEdge>(3);
			foreach (HalfEdge edge in BoundingEdges)
			{
				if (edge.Constraint || edge.Pair.Constraint) { continue; }

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
