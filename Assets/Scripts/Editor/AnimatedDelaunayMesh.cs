using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Delaunay
{
	public class AnimatedDelaunayMesh : DelaunayMesh
	{
		public float Transition = 1f;

		public bool IsRunning { get; private set; }

		public bool AnimatedAddObstacle(IEnumerable<Vector3> vertices, Action<Obstacle> onCreate)
		{
			if (IsRunning)
			{
				GameObject.FindObjectOfType<Stage>().StartCoroutine(CoAddObstacle(vertices, onCreate));
				return true;
			}

			Debug.LogError("Can not AddObstacle while a task is running");
			if (onCreate != null) { onCreate(null); }
			return false;
		}

		IEnumerator CoAddObstacle(IEnumerable<Vector3> container, Action<Obstacle> onCreate)
		{
			IsRunning = true;
			IEnumerator<Vector3> e = container.GetEnumerator();
			List<HalfEdge> polygonBoundingEdges = new List<HalfEdge>();

			if (e.MoveNext())
			{
				Vertex prevVertex = geomManager.CreateVertex(e.Current);
				Vertex firstVertex = prevVertex;

				for (; e.MoveNext(); )
				{
					Vertex currentVertex = geomManager.CreateVertex(e.Current);
					yield return CoAddConstraintEdge(polygonBoundingEdges, prevVertex, currentVertex);

					prevVertex = currentVertex;
				}

				yield return CoAddConstraintEdge(polygonBoundingEdges, prevVertex, firstVertex);
			}

			Obstacle obstacle = geomManager.CreateObstacle(polygonBoundingEdges);
			MarkObstacle(obstacle);

			if (onCreate != null)
			{
				onCreate(obstacle);
			}

			IsRunning = false;
		}

		IEnumerator CoAddConstraintEdge(List<HalfEdge> container, Vertex src, Vertex dest)
		{
			if (Append(src))
			{
				yield return new WaitForSeconds(Transition);
			}

			if (Append(dest))
			{
				yield return new WaitForSeconds(Transition);
			}

			for (; src != dest; )
			{
				container.Add(AddConstraintAt(ref src, dest));
				yield return new WaitForSeconds(Transition);
			}
		}
	}
}
