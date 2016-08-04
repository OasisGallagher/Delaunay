// http://www.flipcode.com/archives/The_Half-Edge_Data_Structure.shtml

using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace Delaunay
{
	/// <summary>
	/// 边.
	/// </summary>
	public class HalfEdge
	{
		public static IDGenerator HalfEdgeIDGenerator = new IDGenerator();

		public HalfEdge()
		{
			ID = HalfEdgeIDGenerator.Value;
		}

		public int ID { get; set; }

		/// <summary>
		/// 边的终点.
		/// </summary>
		public Vertex Dest { get; set; }

		/// <summary>
		/// 下一条边.
		/// </summary>
		public HalfEdge Next { get; set; }

		/// <summary>
		/// 上一条边.
		/// </summary>
		public HalfEdge Prev
		{
			get { return Pair.Next; }
		}

		/// <summary>
		/// 边的"另一半".
		/// </summary>
		public HalfEdge Pair;

		/// <summary>
		/// 边所包围的三角形.
		/// </summary>
		public Triangle Face { get; set; }

		/// <summary>
		/// 是否为约束边.
		/// </summary>
		public bool Constrained { get; set; }

		/// <summary>
		/// 边的起点.
		/// </summary>
		public Vertex Src
		{
			get { return Pair.Dest; }
		}

		/// <summary>
		/// 通过边的Next构成的环.
		/// </summary>
		public List<HalfEdge> Cycle
		{
			get { return GetEdgeCycle(); }
		}

		/// <summary>
		/// 环形连接list内的边.
		/// </summary>
		public HalfEdge CycleLink(params HalfEdge[] list)
		{
			Utility.Verify(list != null && list.Length > 0);

			HalfEdge current = this;
			foreach (HalfEdge e in list)
			{
				current.Next = e;
				current = e;
			}

			current.Next = this;
			return this;
		}

		/// <summary>
		/// 序列化边.
		/// </summary>
		public void WriteBinary(BinaryWriter writer)
		{
			writer.Write(ID);
			writer.Write(Dest != null ? Dest.ID : -1);
			writer.Write(Next != null ? Next.ID : -1);
			writer.Write(Pair != null ? Pair.ID : -1);
			writer.Write(Constrained);
		}

		/// <summary>
		/// 反序列化边.
		/// </summary>
		public void ReadBinary(BinaryReader reader, List<Vertex> vertices, IDictionary<int, HalfEdge> container)
		{
			container[ID] = this;

			int destVertexID = reader.ReadInt32();

			Dest = vertices.Find(item => { return item.ID == destVertexID; });
			Utility.Verify(Dest != null);

			int nextEdge = reader.ReadInt32();

			HalfEdge edge = null;
			if (nextEdge != -1 && !container.TryGetValue(nextEdge, out edge))
			{
				container.Add(nextEdge, edge = new HalfEdge());
				edge.ID = nextEdge;
			}
			Next = edge;

			int pairEdge = reader.ReadInt32();

			if (!container.TryGetValue(pairEdge, out edge))
			{
				container.Add(pairEdge, edge = new HalfEdge());
				edge.ID = pairEdge;
			}
			Pair = edge;

			Utility.Verify(Pair != null);

			Constrained = reader.ReadBoolean();

			// Face字段由Triangle来更新.
		}

		public override string ToString()
		{
			return ID + "_" + Pair.Dest + "=>" + Dest;
		}

		List<HalfEdge> GetEdgeCycle()
		{
			List<HalfEdge> answer = new List<HalfEdge> { this };
			for (HalfEdge current = this; (current = current.Next) != this; )
			{
				if (current == null) { throw new ArgumentNullException("Invalid cycle"); }
				answer.Add(current);
			}

			return answer;
		}
	}
}
