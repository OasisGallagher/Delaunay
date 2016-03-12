using System.Collections.Generic;
using System.Xml;
using UnityEngine;

namespace Delaunay
{
	public class Triangle : MonoBehaviour
	{
		public int ID { get; private set; }

		public static Triangle Create(Triangle src)
		{
			GameObject go = new GameObject();

			Triangle answer = go.AddComponent<Triangle>();
			answer.Edge = src.Edge;

			return answer;
		}

		public static Triangle Create(XmlReader reader, IDictionary<int, HalfEdge> container)
		{
			GameObject go = new GameObject();

			Triangle answer = go.AddComponent<Triangle>();

			answer.ReadXml(reader, container);
			return answer;
		}

		public static Triangle Create(Vertex a, Vertex b, Vertex c)
		{
			HalfEdge ab = HalfEdge.Create(a, b);
			HalfEdge bc = HalfEdge.Create(b, c);
			HalfEdge ca = HalfEdge.Create(c, a);

			if (ab.Face != null)
			{
				Utility.Verify(ab.Face == bc.Face && bc.Face == ca.Face);
				return ab.Face;
			}

			ab.Next = bc;
			bc.Next = ca;
			ca.Next = ab;

			GameObject go = new GameObject();

			Triangle answer = go.AddComponent<Triangle>();

			ab.Face = bc.Face = ca.Face = answer;
			answer.Edge = ab;

			return answer;
		}

		public static void Release(Triangle triangle)
		{
			triangle.BoundingEdges.ForEach(e => 
			{
				if (e.Face == triangle)
				{
					e.Face = null;
					e.Next = null;
				}
			});

			triangle.Edge = null;
			GameObject.DestroyImmediate(triangle.gameObject);
		}

		public static void ResetIDGenerator() { triangleID = 0; }

		void Awake()
		{
			ID = triangleID++;
			gameObject.name = "Triangle_" + ID;
		}

		void Start()
		{
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
				if (float.IsNaN(widthA)) { widthA = CalcWidth(a, b); }
				return widthA;
			}

			if (v == B)
			{
				if (float.IsNaN(widthB)) { widthB = CalcWidth(a, b); }
				return widthB;
			}

			if (v == C)
			{
				if (float.IsNaN(widthC)) { widthC = CalcWidth(a, b); }
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
				halfEdge = value;
				widthA = widthB = widthC = float.NaN;
			}
		}

		public bool Walkable
		{
			get { return walkable; }
			set
			{
				walkable = value;
			}
		}

		public Vector3 Center
		{
			get { return (A.Position + B.Position + C.Position) / 3f; }
		}

		public Vector3 CircumCircleCenter
		{
			get
			{
				float t1 = A.Position.x * A.Position.x + A.Position.z * A.Position.z;
				float t2 = B.Position.x * B.Position.x + B.Position.z * B.Position.z;
				float t3 = C.Position.x * C.Position.x + C.Position.z * C.Position.z;
				float tmp = A.Position.x * B.Position.z + B.Position.x * C.Position.z + C.Position.x * A.Position.z - A.Position.x * C.Position.z - B.Position.x * A.Position.z - C.Position.x * B.Position.z;
				tmp *= 2f;

				float x = (t2 * C.Position.z + t1 * B.Position.z + t3 * A.Position.z - t2 * A.Position.z - t3 * B.Position.z - t1 * C.Position.z) / tmp;
				float z = (t3 * B.Position.x + t2 * A.Position.x + t1 * C.Position.x - t1 * B.Position.x - t2 * C.Position.x - t3 * A.Position.x) / tmp;
				return new Vector3(x, (A.Position.y + B.Position.y + C.Position.y) / 3f, z);
			}
		}

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
			widthA = CalcWidth(AB, CA);
			widthB = CalcWidth(BC, AB);
			widthC = CalcWidth(CA, BC);
		}

		Vertex GetIntersectVertex(HalfEdge ea, HalfEdge eb)
		{
			if (ea.Src == eb.Dest) { return ea.Src; }
			if (ea.Dest == eb.Src) { return ea.Dest; }
			Utility.Verify(false, "Edge {0} and {1} has no intersection", ea, eb);
			return null;
		}

		float CalcWidth(HalfEdge ea, HalfEdge eb)
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

		public bool PointInCircumCircle(Vertex v)
		{
			return MathUtility.PointInCircumCircle(A, B, C, v);
		}

		public void WriteXml(XmlWriter writer)
		{
			writer.WriteAttributeString("ID", ID.ToString());
			using (new XmlWriterScope(writer, "EdgeID"))
			{
				writer.WriteString(Edge != null ? Edge.ID.ToString() : "-1");
			}

			using (new XmlWriterScope(writer, "Walkable"))
			{
				writer.WriteString(Walkable ? "1" : "0");
			}
		}

		#region Pathfinding
		public HalfEdge Portal { get; set; }

		public HalfEdge[] AdjPortals { get { return GetAdjPortals(); } }

		public float G { get; set; }

		public float H { get; set; }
		#endregion

		void ReadXml(XmlReader reader, IDictionary<int, HalfEdge> container)
		{
			ID = int.Parse(reader["ID"]);

			reader.Read();

			int edge = reader.ReadElementContentAsInt();

			Edge = container[edge];

			BoundingEdges.ForEach(e => { e.Face = this; });

			Walkable = reader.ReadElementContentAsBoolean();
		}

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

		bool walkable = true;
		HalfEdge halfEdge = null;

		float widthA = float.NaN;
		float widthB = float.NaN;
		float widthC = float.NaN;

		static int triangleID = 0;
	}
}
