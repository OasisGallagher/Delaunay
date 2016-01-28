using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Delaunay
{
	public static class HalfEdgeContainer
	{
		static Dictionary<Vertex, List<HalfEdge>> container = new Dictionary<Vertex, List<HalfEdge>>();

		public static void Add(HalfEdge edge)
		{
			List<HalfEdge> list = null;
			if (!container.TryGetValue(edge.Src, out list))
			{
				container.Add(edge.Src, list = new List<HalfEdge>());
			}

			list.Add(edge);
		}

		public static void Remove(HalfEdge edge)
		{
			List<HalfEdge> list = container[edge.Src];
			Utility.Verify(list.Remove(edge));
			if (list.Count == 0)
			{
				container.Remove(edge.Src);
			}
		}

		public static List<HalfEdge> GetRays(Vertex vertex)
		{
			List<HalfEdge> answer = null;
			container.TryGetValue(vertex, out answer);
			return answer ?? new List<HalfEdge>();
		}

		public static List<Vertex> Vertices
		{
			get { return new List<Vertex>(container.Keys); }
		}
	}

	public class HalfEdge
	{
		public static HalfEdge Create(Vertex src, Vertex dest)
		{
			HalfEdge self = HalfEdgeContainer.GetRays(src).Find(item => { return item.Dest == dest; });

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

				HalfEdgeContainer.Add(self);
				HalfEdgeContainer.Add(other);
			}

			return self;
		}

		public static void Release(HalfEdge edge)
		{
			HalfEdgeContainer.Remove(edge.Pair);
			HalfEdgeContainer.Remove(edge);
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
		public Triangle Face;

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

		public bool Forward
		{
			get
			{
				return !Mathf.Approximately(Src.Position.x, Dest.Position.x)
					? Src.Position.x < Dest.Position.x : Src.Position.z < Src.Position.z;
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
				if (answer.Count >= EditorConstants.kDebugInvalidCycle)
				{
					Debug.LogError("Too many edges!");
					break;
				}
			}

			return answer;
		}

		bool isConstraint;

		static int halfEdgeID = 0;
	}
}
