using System.Collections.Generic;
using System.Xml;
using UnityEngine;

namespace Delaunay
{
	public class Obstacle
	{
		public int ID { get; private set; }

		public static IDGenerator ObstacleIDGenerator = new IDGenerator();

		public Obstacle()
		{
			ID = ObstacleIDGenerator.Value;
		}

		public List<HalfEdge> BoundingEdges
		{
			get { return boundingEdges; }
			set
			{
				boundingEdges = value;
				mesh = CalculateMeshTriangles(boundingEdges);
			}
		}

		public List<Triangle> Mesh { get { return mesh; } }

		public void WriteXml(XmlWriter writer)
		{
			writer.WriteAttributeString("ID", ID.ToString());

			writer.WriteStartElement("BoundingEdges");
			foreach (HalfEdge edge in BoundingEdges)
			{
				writer.WriteStartElement("EdgeID");
				writer.WriteString(edge.ID.ToString());
				writer.WriteEndElement();
			}
			writer.WriteEndElement();
		}

		public void ReadXml(XmlReader reader, IDictionary<int, HalfEdge> container)
		{
			ID = int.Parse(reader["ID"]);
			reader.Read();

			List<HalfEdge> bounding = new List<HalfEdge>();

			reader.Read();

			for (; reader.Name != "BoundingEdges"; )
			{
				int halfEdge = reader.ReadElementContentAsInt();
				bounding.Add(container[halfEdge]);
			}

			BoundingEdges = bounding;
		}

		List<Triangle> CalculateMeshTriangles(List<HalfEdge> edges)
		{
			Vector3[] boundings = new Vector3[edges.Count];
			edges.transform(boundings, item => { return item.Src.Position; });

			List<Triangle> answer = new List<Triangle>();

			Queue<HalfEdge> queue = new Queue<HalfEdge>();
			queue.Enqueue(boundingEdges[0]);
			for (; queue.Count > 0; )
			{
				HalfEdge edge = queue.Dequeue();
				if (edge.Face == null) { continue; }
				if (answer.Contains(edge.Face)) { continue; }

				answer.Add(edge.Face);

				HalfEdge e1 = edge.Face.BC, e2 = edge.Face.CA;
				if (edge == edge.Face.BC) { e1 = edge.Face.AB; e2 = edge.Face.CA; }
				if (edge == edge.Face.CA) { e1 = edge.Face.AB; e2 = edge.Face.BC; }

				if (!e1.Constraint) { queue.Enqueue(e1.Pair); }
				if (!e2.Constraint) { queue.Enqueue(e2.Pair); }
			}

			return answer;
		}

		List<Triangle> mesh = null;
		List<HalfEdge> boundingEdges = null;
	}
}
