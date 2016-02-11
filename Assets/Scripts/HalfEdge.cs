using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Delaunay
{
	public class HalfEdge : IXmlSerializable
	{
		public static HalfEdge Create(Vertex src, Vertex dest)
		{
			HalfEdge self = GeomManager.GetRays(src).Find(item => { return item.Dest == dest; });

			if (self == null)
			{
				self = new HalfEdge();
				self.Dest = dest;

				HalfEdge other = new HalfEdge();
				other.Dest = src;

				self.Pair = other;
				other.Pair = self;

				src.Edge = self;
				dest.Edge = other;

				GeomManager.Add(self);
				GeomManager.Add(other);
			}

			return self;
		}

		public static HalfEdge Create(XmlReader reader)
		{
			HalfEdge answer = new HalfEdge();
			answer.ReadXml(reader);
			return answer;
		}

		public static void Release(HalfEdge edge)
		{
			GeomManager.Remove(edge.Pair);
			GeomManager.Remove(edge);
		}

		public int ID { get; private set; }

		/// <summary>
		/// Vertex at the end of the half-edge.
		/// </summary>
		public Vertex Dest;

		/// <summary>
		/// Next half-edge around the face.
		/// </summary>
		public HalfEdge Next;

		/// <summary>
		/// Oppositely oriented adjacent half-edge.
		/// </summary>
		public HalfEdge Pair;

		/// <summary>
		/// Face the half-edge borders.
		/// </summary>
		public Triangle Face
		{
			get { return face; }
			set
			{
				if (face == value) { return; }
				face = value;
				if (face == null && Pair.face == null)
				{
					HalfEdge.Release(this);
				}
			}
		}

		HalfEdge() { ID = halfEdgeID++; }

		public bool Constraint
		{
			get { return isConstraint; }
			set
			{
				if (isConstraint == value) { return; }
				Pair.isConstraint = isConstraint = value;
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

		public XmlSchema GetSchema()
		{
			throw new NotImplementedException();
		}

		public void ReadXml(XmlReader reader)
		{
			ID = int.Parse(reader["ID"]);
			// TODO: INIT DEST VERTEX ID.

			//
		}

		public void WriteXml(XmlWriter writer)
		{
			writer.WriteAttributeString("ID", ID.ToString());

			using (new XmlWriterScope(writer, "DestVertexID"))
			{
				writer.WriteString(Dest != null ? Dest.ID.ToString() : "-1");
			}

			using (new XmlWriterScope(writer, "NextEdgeID"))
			{
				writer.WriteString(Next != null ? Next.ID.ToString() : "-1");
			}

			using (new XmlWriterScope(writer, "PairEdgeID"))
			{
				writer.WriteString(Pair != null ? Pair.ID.ToString() : "-1");
			}

			using (new XmlWriterScope(writer, "FaceID"))
			{
				writer.WriteString(Face != null ? Face.ID.ToString() : "-1");
			}

			using (new XmlWriterScope(writer, "Constraint"))
			{
				writer.WriteString(Constraint.ToString());
			}
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

		bool isConstraint;
		Triangle face;

		static int halfEdgeID = 0;
	}
}
