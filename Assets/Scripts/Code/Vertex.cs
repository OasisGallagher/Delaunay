using System.Xml;
using UnityEngine;

namespace Delaunay
{
	public class Vertex
	{
		public int ID;

		/// <summary>
		/// One of the half-edges emanating from this vertex.
		/// </summary>
		/*
		public HalfEdge Edge;
		*/

		public Vector3 Position;

		public static IDGenerator VertexIDGenerator = new IDGenerator();

		public static Vertex Create(Vector3 position)
		{
			Utility.Verify(GeomManager.FindVertex(position) == null, "Duplicate vertex at position " + position);
			Vertex ans = new Vertex(position);
			GeomManager.AddVertex(ans);
			return ans;
		}

		public static Vertex Create(XmlReader reader)
		{
			Vertex ans = new Vertex(Vector3.zero);
			ans.ReadXml(reader);
			GeomManager.AddVertex(ans);
			return ans;
		}

		Vertex(Vector3 position)
		{
			ID = VertexIDGenerator.Value;
			this.Position = position;
		}

		public override string ToString()
		{
			return ID + "@" + Position;
		}

		public override bool Equals(object obj)
		{
			return obj is Vertex && (obj as Vertex).Position.equals2(Position);
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		public void WriteXml(XmlWriter writer)
		{
			writer.WriteAttributeString("ID", ID.ToString());

			writer.WriteStartElement("Position");
			writer.WriteAttributeString("X", Position.x.ToString());
			writer.WriteAttributeString("Y", Position.y.ToString());
			writer.WriteAttributeString("Z", Position.z.ToString());
			writer.WriteEndElement();

			/*using (new XmlWriterScope(writer, "EdgeID"))
			{
				writer.WriteString(Edge != null ? Edge.ID.ToString() : "-1");
			}*/
		}

		void ReadXml(XmlReader reader)
		{
			ID = int.Parse(reader["ID"]);
			reader.Read();
			Position.Set(float.Parse(reader["X"]), float.Parse(reader["Y"]), float.Parse(reader["Z"]));
		}
	}
}
