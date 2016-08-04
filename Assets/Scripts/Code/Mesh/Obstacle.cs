using System.Collections.Generic;
using System.IO;

namespace Delaunay
{
	/// <summary>
	/// 障碍物.
	/// </summary>
	public class Obstacle
	{
		public int ID { get; private set; }

		public static IDGenerator ObstacleIDGenerator = new IDGenerator();

		public Obstacle()
		{
			ID = ObstacleIDGenerator.Value;
		}

		/// <summary>
		/// 障碍物的包围边.
		/// </summary>
		public List<HalfEdge> BoundingEdges
		{
			get { return boundingEdges; }
			set
			{
				boundingEdges = value;
				mesh = CalculateMeshTriangles(boundingEdges);
			}
		}

		/// <summary>
		/// 组成该障碍物三角网格.
		/// </summary>
		public List<Triangle> Mesh { get { return mesh; } }

		/// <summary>
		/// 序列化障碍物.
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
		/// 反序列化障碍物.
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

		/// <summary>
		/// 计算edges包围的几何图形的三角网格.
		/// </summary>
		List<Triangle> CalculateMeshTriangles(List<HalfEdge> edges)
		{
			List<Triangle> answer = new List<Triangle>();

			Queue<HalfEdge> queue = new Queue<HalfEdge>();
			queue.Enqueue(edges[0]);
			for (; queue.Count > 0; )
			{
				HalfEdge edge = queue.Dequeue();
				if (edge.Face == null) { continue; }
				if (answer.Contains(edge.Face)) { continue; }

				answer.Add(edge.Face);

				HalfEdge e1 = edge.Next, e2 = e1.Next;
				/*
				HalfEdge e1 = edge.Face.BC, e2 = edge.Face.CA;
				if (edge == edge.Face.BC) { e1 = edge.Face.AB; e2 = edge.Face.CA; }
				if (edge == edge.Face.CA) { e1 = edge.Face.AB; e2 = edge.Face.BC; }
				*/
				if (!e1.Constrained && !e1.Pair.Constrained) { queue.Enqueue(e1.Pair); }
				if (!e2.Constrained && !e2.Pair.Constrained) { queue.Enqueue(e2.Pair); }
			}

			return answer;
		}

		List<Triangle> mesh = null;
		List<HalfEdge> boundingEdges = null;
	}
}
