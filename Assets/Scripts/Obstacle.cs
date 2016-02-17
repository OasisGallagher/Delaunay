using System.Collections.Generic;
using UnityEngine;

namespace Delaunay
{
	public class Obstacle
	{
		public int ID { get; private set; }

		public bool __tmpActive = true;

		public static Obstacle Create(List<HalfEdge> boundingEdges)
		{
			Obstacle answer = new Obstacle();
			answer.BoundingEdges = boundingEdges;
			GeomManager.AddObstacle(answer);
			return answer;
		}

		public void Release(Obstacle obstacle)
		{
			GeomManager.RemoveObstacle(obstacle);
		}

		Obstacle()
		{
			ID = obstacleID++;
		}

		public List<HalfEdge> BoundingEdges
		{
			get { return boundingEdges; }
			set
			{
				boundingEdges = value;
				mesh = GetMeshTriangles(boundingEdges);
			}
		}

		public List<Triangle> Mesh { get { return mesh; } }

		List<Triangle> GetMeshTriangles(List<HalfEdge> edges)
		{
			List<Vector3> boundings = new List<Vector3>(edges.Count);
			edges.ForEach(item => { boundings.Add(item.Src.Position); });

			List<Triangle> answer = new List<Triangle>();

			Queue<HalfEdge> queue = new Queue<HalfEdge>();
			queue.Enqueue(boundingEdges[0]);
			for (; queue.Count > 0; )
			{
				HalfEdge edge = queue.Dequeue();
				if (edge.Face == null) { continue; }

				if (!Utility.PolygonContains(boundings, edge.Face.GetOpposite(edge).Position))
				{
					continue;
				}
				/*
				Vertex oppositeVertex = edge.Face.GetOpposite(edge);
				if (edges.Find(item => { return item.Src == oppositeVertex; }) == null)
				{
					continue;
				}
				*/
				if (answer.Contains(edge.Face)) { continue; }
				answer.Add(edge.Face);

				HalfEdge e1 = edge.Face.BC, e2 = edge.Face.CA;
				if (edge == edge.Face.BC) { e1 = edge.Face.AB; e2 = edge.Face.CA; }
				if (edge == edge.Face.CA) { e1 = edge.Face.AB; e2 = edge.Face.BC; }

				queue.Enqueue(e1.Pair);
				queue.Enqueue(e2.Pair);
			}

			return answer;
		}

		List<Triangle> mesh = null;
		List<HalfEdge> boundingEdges = null;
		static int obstacleID = 0;
	}
}
