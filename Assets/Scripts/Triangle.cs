using System.Collections.Generic;
using System.Xml;
using UnityEngine;

namespace Delaunay
{
	public class Triangle : MonoBehaviour, IPathNode
	{
		public int ID { get; private set; }

		public static Triangle Create(Triangle src)
		{
			GameObject go = new GameObject();

			go.AddComponent<MeshFilter>();
			go.AddComponent<MeshRenderer>();

			Triangle answer = go.AddComponent<Triangle>();

			answer.CopyFrom(src);
			return answer;
		}

		public static Triangle Create(XmlReader reader, IDictionary<int, HalfEdge> container)
		{
			GameObject go = new GameObject();

			go.AddComponent<MeshFilter>();
			go.AddComponent<MeshRenderer>();

			Triangle answer = go.AddComponent<Triangle>();

			answer.ReadXml(reader, container);
			return answer;
		}

		public static Triangle Create(Vertex a, Vertex b, Vertex c)
		{
			HalfEdge ab = HalfEdge.Create(a, b);
			HalfEdge bc = HalfEdge.Create(b, c);
			HalfEdge ca = HalfEdge.Create(c, a);

			ab.Next = bc;
			bc.Next = ca;
			ca.Next = ab;

			GameObject go = new GameObject();

			go.AddComponent<MeshFilter>();
			go.AddComponent<MeshRenderer>();

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
			Vector3 position = Edge.Dest.Position;
			gameObject.transform.position = position;

			MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();

			meshFilter.mesh.vertices = new Vector3[] {
				A.Position - position + EditorConstants.kTriangleMeshOffset, 
				B.Position - position + EditorConstants.kTriangleMeshOffset, 
				C.Position - position + EditorConstants.kTriangleMeshOffset 
			};

			meshFilter.mesh.triangles = new int[] { 0, 2, 1 };
			meshFilter.mesh.RecalculateNormals();

			meshFilter.mesh.uv = EditorConstants.kUV;

			UpdateWalkableMaterial();
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

		public HalfEdge GetOpposite(Vertex from)
		{
			foreach (HalfEdge edge in BoundingEdges)
			{
				if (!Utility.Equals2D(from.Position, edge.Src.Position)
					&& !Utility.Equals2D(from.Position, edge.Dest.Position))
				{
					return edge.Pair;
				}
			}

			return null;
		}

		/// <summary>
		/// One of the half-edges bordering the face.
		/// </summary>
		public HalfEdge Edge { get; set; }

		public bool Walkable
		{
			get { return walkable; }
			set
			{
				if (value == walkable) { return; }
				walkable = value;
				UpdateWalkableMaterial();
			}
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

		public bool Contains(Vertex t)
		{
			return Utility.PolygonContains(new Vector3[] { A.Position, B.Position, C.Position }, t.Position);
		}

		public int GetPointDirection(Vector3 point)
		{
			float t0 = Utility.Cross2D(point, B.Position, A.Position);
			if (t0 > 0) { return -1; }

			if (Mathf.Approximately(t0, 0)
				&& Utility.DiagonalRectContains(point, B.Position, A.Position))
			{
				return 1;
			}

			float t1 = Utility.Cross2D(point, C.Position, B.Position);
			if (t1 > 0) { return -2; }

			if (Mathf.Approximately(t1, 0)
				&& Utility.DiagonalRectContains(point, C.Position, B.Position))
			{
				return 2;
			}

			float t2 = Utility.Cross2D(point, A.Position, C.Position);
			if (t2 > 0) { return -3; }

			if (Mathf.Approximately(t2, 0)
				&& Utility.DiagonalRectContains(point, A.Position, C.Position))
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
			return Utility.Equals2D(A, v) || Utility.Equals2D(B, v) || Utility.Equals2D(C, v);
		}

		public bool HasVertex(Vector3 position)
		{
			return Utility.Equals2D(A.Position, position)
				|| Utility.Equals2D(B.Position, position) || Utility.Equals2D(C.Position, position);
		}

		public bool PointInCircumCircle(Vertex v)
		{
			return Utility.PointInCircumCircle(A, B, C, v);
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
		public HalfEdge Entry { get; set; }
		public List<HalfEdge> AdjNodes
		{
			get
			{
				// TODO:
				System.Action<List<HalfEdge>, HalfEdge> lamda = (container, edge) =>
				{
					if (!edge.Constraint && edge.Pair.Face != null && edge.Pair.Face.Walkable)
					{
						container.Add(edge.Pair);
					}
				};

				List<HalfEdge> answer = new List<HalfEdge>();

				lamda(answer, AB);
				lamda(answer, BC);
				lamda(answer, CA);

				return answer;
			}
		}

		public Vector3 Center
		{
			get { return (A.Position + B.Position + C.Position) / 3f; }
		}

		public float F { get { return G + H; } }
		public float G { get; set; }
		public float H { get; set; }

		public float CalcWeight(HalfEdge other)
		{
			if (!AdjNodes.Contains(other))
			{
				return float.PositiveInfinity;
			}

			return (Center - other.Face.Center).magnitude;
		}

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

		void CopyFrom(Triangle src)
		{
			Edge = src.Edge;
		}

		void UpdateWalkableMaterial()
		{
			MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>();
			meshRenderer.material = Walkable ? EditorConstants.kWalkableMaterial : EditorConstants.kBlockMaterial;
		}

		bool walkable = true;

		static int triangleID = 0;
	}

	[UnityEditor.CustomEditor(typeof(Triangle))]
	public class TriangleEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			Triangle triangle = target as Triangle;
			triangle.Walkable = GUILayout.Toggle(triangle.Walkable, "Walkable");
			GUILayout.Label("A:" + triangle.A);
			GUILayout.Label("B:" + triangle.B);
			GUILayout.Label("C:" + triangle.C);
		}
	}
}
