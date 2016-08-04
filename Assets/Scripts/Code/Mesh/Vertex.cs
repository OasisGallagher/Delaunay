using System.IO;
using UnityEngine;

namespace Delaunay
{
	/// <summary>
	/// 顶点.
	/// </summary>
	public class Vertex
	{
		public static IDGenerator VertexIDGenerator = new IDGenerator();

		public Vertex(Vector3 position)
		{
			ID = VertexIDGenerator.Value;
			this.Position = position;
		}

		public int ID;

		/// <summary>
		/// One of the half-edges emanating from this vertex.
		/// </summary>
		/*
		public HalfEdge Edge;
		*/

		public Vector3 Position;

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

		/// <summary>
		/// 序列化顶点.
		/// </summary>
		public void WriteBinary(BinaryWriter writer)
		{
			writer.Write(ID);
			writer.write(Position);
		}

		/// <summary>
		/// 反序列化顶点.
		/// </summary>
		public void ReadBinary(BinaryReader reader)
		{
			ID = reader.ReadInt32();
			Position = reader.readVector3();
		}
	}
}
