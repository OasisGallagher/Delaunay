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
			answer.boundingEdges = boundingEdges;
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

		public List<HalfEdge> BoundingEdges { get { return boundingEdges; } }

		/// <summary>
		/// TODO: Iterate....
		/// </summary>
		public List<Triangle> Mesh
		{
			get
			{
				List<Triangle> answer = new List<Triangle>();
				Vector3[] boundings = new Vector3[BoundingEdges.Count];

				int index = 0;
				BoundingEdges.ForEach(item => {boundings[index++] = item.Src.Position;});

				foreach (HalfEdge edge in BoundingEdges)
				{
					foreach (HalfEdge ray in GeomManager.GetRays(edge.Src))
					{
						if (ray.Face == null) { continue; }
						bool contains = true;

						foreach (HalfEdge faceBounding in ray.Face.BoundingEdges)
						{
							if (!Utility.PolygonContains(boundings, faceBounding.Src.Position))
							{
								contains = false;
								break;
							}
						}

						if (contains) { answer.Add(ray.Face); }
					}
				}

				return answer;
			}
		}

		List<HalfEdge> boundingEdges = null;
		static int obstacleID = 0;
	}
}
