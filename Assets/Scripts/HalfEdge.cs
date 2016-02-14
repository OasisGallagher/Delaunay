﻿using System;
using System.Collections.Generic;
using System.Xml;

namespace Delaunay
{
	public class HalfEdge
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

				/*src.Edge = self;
				dest.Edge = other;
				*/
// 
// 				if (self.ID == 364 || other.ID == 364)
// 				{
// 					UnityEngine.Debug.Log("364");
// 				}
// 
// 				HalfEdge target = GeomManager.AllEdges.Find(item =>
// 				{
// 					if (item == self || item == other) { return false; }
// 					if (item.ID == 359)
// 					{
// 						UnityEngine.Debug.Log("359");
// 					}
// 
// 					var trace = new System.Diagnostics.StackTrace();
// 					if (trace.GetFrame(4).ToString().Contains("InsertOnEdge"))
// 					{
// 						return false;
// 					}
// 
// 					UnityEngine.Vector2 answer = UnityEngine.Vector2.zero;
// 					return Utility.SegmentCross(out answer, src.Position, dest.Position, item.Src.Position, item.Dest.Position) == LineCrossState.FullyOverlaps;
// 				});

//				if (target != null)
// 				{
// 					UnityEngine.Debug.LogError(string.Format("edge {0} overlaps another edge {1}", self.ID, target.ID));
//				}

				GeomManager.Add(self);
				GeomManager.Add(other);
			}

			return self;
		}

		public static HalfEdge Create(XmlReader reader, IDictionary<int, HalfEdge> container)
		{
			HalfEdge answer = null;
			int edgeID = int.Parse(reader["ID"]);
			reader.Read();

			if (!container.TryGetValue(edgeID, out answer))
			{
				container.Add(edgeID, answer = new HalfEdge());
				answer.ID = edgeID;
			}

			answer.ReadXml(reader, container);
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
		public HalfEdge Next
		{
			get { return mNext; }
			set
			{
				if (value == mNext) { return; }
				if (Face != null && Face.Edge == this)
				{
					if (Next.Face == Face) { Face.Edge = Next; }
					else if (Prev != null && Prev.Face == Face) { Face.Edge = Prev; }
					else { Face.Edge = null; }
				}

				mNext = value;
			}
		}

		public HalfEdge Prev
		{
			get { return Pair.Next; }
		}

		HalfEdge mNext;

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

		HalfEdge()
		{
			ID = halfEdgeID++;
		}

		public static void ResetIDGenerator() { halfEdgeID = 0; }

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

			using (new XmlWriterScope(writer, "Constraint"))
			{
				writer.WriteString(Constraint ? "1" : "0");
			}
		}

		public override string ToString()
		{
			return ID + "_" + Pair.Dest + "=>" + Dest;
		}

		void ReadXml(XmlReader reader, IDictionary<int, HalfEdge> container)
		{
			container[ID] = this;

			int destVertexID = reader.ReadElementContentAsInt();

			Dest = GeomManager.AllVertices.Find(item => { return item.ID == destVertexID; });
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

		static int halfEdgeID = 0;
	}
}
