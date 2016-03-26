using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace Delaunay
{
	public class BorderSet
	{
		public int ID { get; private set; }

		public static IDGenerator BorderSetIDGenerator = new IDGenerator();

		public BorderSet()
		{
			ID = BorderSetIDGenerator.Value;
		}

		public List<HalfEdge> BoundingEdges { get; set; }

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

		public void WriteBinary(BinaryWriter writer)
		{
			writer.Write(ID);
			writer.Write(BoundingEdges.Count);
			foreach (HalfEdge edge in BoundingEdges)
			{
				writer.Write(edge.ID);
			}
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

		public void ReadBinary(BinaryReader reader, IDictionary<int, HalfEdge> container)
		{
			ID = reader.ReadInt32();

			int count = reader.ReadInt32();
			List<HalfEdge> bounding = new List<HalfEdge>(count);

			for (int i = 0; i < count; ++i)
			{
				int halfEdge = reader.ReadInt32();
				bounding.Add(container[halfEdge]);
			}

			BoundingEdges = bounding;
		}
	}
}