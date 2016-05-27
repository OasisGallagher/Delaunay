using System.Collections.Generic;
using System.IO;

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

		public void WriteBinary(BinaryWriter writer)
		{
			writer.Write(ID);
			writer.Write(BoundingEdges.Count);
			foreach (HalfEdge edge in BoundingEdges)
			{
				writer.Write(edge.ID);
			}
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