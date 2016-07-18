// http://www.flipcode.com/archives/The_Half-Edge_Data_Structure.shtml

using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace Delaunay
{
	public class HalfEdge
	{
		public static IDGenerator HalfEdgeIDGenerator = new IDGenerator();

		public HalfEdge()
		{
			ID = HalfEdgeIDGenerator.Value;
		}

		public int ID { get; set; }

		/// <summary>
		/// Vertex at the end of the half-edge.
		/// </summary>
		public Vertex Dest { get; set; }

		/// <summary>
		/// Next half-edge around the face.
		/// </summary>
		public HalfEdge Next { get; set; }

		public HalfEdge Prev
		{
			get { return Pair.Next; }
		}

		/// <summary>
		/// Oppositely oriented adjacent half-edge.
		/// </summary>
		public HalfEdge Pair;

		/// <summary>
		/// Face the half-edge borders.
		/// </summary>
		public Triangle Face { get; set; }

		public Vector3 Center { get { return (Src.Position + Dest.Position) / 2f; } }

		public bool Constrained { get; set; }

		public Vertex Src
		{
			get { return Pair.Dest; }
		}

		public List<HalfEdge> Cycle
		{
			get { return GetEdgeCycle(); }
		}

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

		public void WriteBinary(BinaryWriter writer)
		{
			writer.Write(ID);
			writer.Write(Dest != null ? Dest.ID : -1);
			writer.Write(Next != null ? Next.ID : -1);
			writer.Write(Pair != null ? Pair.ID : -1);
			writer.Write(Constrained);
		}

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

			// Face is updated by Triangle.
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
				Utility.Verify(answer.Count < EditorConstants.kDebugInvalidCycle);
			}

			return answer;
		}
	}
}
