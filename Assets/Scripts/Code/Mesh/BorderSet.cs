using System.Collections.Generic;
using System.IO;

namespace Delaunay
{
	/// <summary>
	/// 边集. 
	/// <para>与Obstacle的区别是: 边集为边的集合, 不一定封闭, 且不将封闭区域标记为不可行走.</para>
	/// </summary>
	public class BorderSet
	{
		public int ID { get; private set; }

		public static IDGenerator BorderSetIDGenerator = new IDGenerator();

		public BorderSet()
		{
			ID = BorderSetIDGenerator.Value;
		}

		/// <summary>
		/// 边集中的成员.
		/// </summary>
		public List<HalfEdge> BoundingEdges { get; set; }

		/// <summary>
		/// 序列化边.
		/// </summary>
		public void WriteBinary(BinaryWriter writer)
		{
			writer.Write(ID);
			writer.Write(BoundingEdges.Count);
			foreach (HalfEdge edge in BoundingEdges)
			{
				writer.Write(edge.ID);
			}
		}

		/// <summary>
		/// 反序列化边.
		/// </summary>
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