using System.IO;
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

		public Vertex(Vector3 position)
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

		public void WriteBinary(BinaryWriter writer)
		{
			writer.Write(ID);
			writer.write(Position);
		}

		public void ReadBinary(BinaryReader reader)
		{
			ID = reader.ReadInt32();
			Position = reader.readVector3();
		}

		public void ReadXml(XmlReader reader)
		{
			ID = int.Parse(reader["ID"]);
			reader.Read();
			Position.Set(float.Parse(reader["X"]), float.Parse(reader["Y"]), float.Parse(reader["Z"]));
		}
	}
}
