using System;
using UnityEngine;
using System.Collections.Generic;
using System.Xml;

namespace Delaunay
{
	public class HalfEdge
	{
		public static IDGenerator HalfEdgeIDGenerator = new IDGenerator();

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

		public HalfEdge()
		{
			ID = HalfEdgeIDGenerator.Value;
		}

		public bool Constraint
		{
			get { return isConstraint; }
			set
			{
				if (isConstraint == value) { return; }
				isConstraint = value;
			}
		}

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

		public void WriteXml(XmlWriter writer)
		{
			writer.WriteAttributeString("ID", ID.ToString());

			writer.WriteStartElement("DestVertexID");
			writer.WriteString(Dest != null ? Dest.ID.ToString() : "-1");
			writer.WriteEndElement();

			writer.WriteStartElement("NextEdgeID");
			writer.WriteString(Next != null ? Next.ID.ToString() : "-1");
			writer.WriteEndElement();

			writer.WriteStartElement("PairEdgeID");
			writer.WriteString(Pair != null ? Pair.ID.ToString() : "-1");
			writer.WriteEndElement();

			writer.WriteStartElement("Constraint");
			writer.WriteString(Constraint ? "1" : "0");
			writer.WriteEndElement();
		}

		public override string ToString()
		{
			return ID + "_" + Pair.Dest + "=>" + Dest;
		}

		public void ReadXml(XmlReader reader, List<Vertex> vertices, IDictionary<int, HalfEdge> container)
		{
			container[ID] = this;

			int destVertexID = reader.ReadElementContentAsInt();

			Dest = vertices.Find(item => { return item.ID == destVertexID; });
			Utility.Verify(Dest != null);

			int nextEdge = reader.ReadElementContentAsInt();

			HalfEdge edge = null;
			if (nextEdge != -1 && !container.TryGetValue(nextEdge, out edge))
			{
				container.Add(nextEdge, edge = new HalfEdge());
				edge.ID = nextEdge;
			}
			Next = edge;

			int pairEdge = reader.ReadElementContentAsInt();

			if (!container.TryGetValue(pairEdge, out edge))
			{
				container.Add(pairEdge, edge = new HalfEdge());
				edge.ID = pairEdge;
			}
			Pair = edge;

			Utility.Verify(Pair != null);

			isConstraint = reader.ReadElementContentAsBoolean();

			// Face is updated by Triangle.
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

		bool isConstraint;
		Triangle face;
	}
}
