using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Delaunay
{
	public class AnimatedDelaunayMesh : DelaunayMesh
	{
		public float Transition = 1f;

		public bool AnimatedAddObstacle(IEnumerable<Vector3> vertices, Action<Obstacle> onCreate)
		{
			if (EditorCoroutineRunner.Instance.Count == 0)
			{
				EditorCoroutineRunner.Instance.StartEditorCoroutine(CoAddObstacle(vertices, onCreate));
				return true;
			}

			Debug.LogError("Can not AddObstacle while a task is running");
			if (onCreate != null) { onCreate(null); }
			
			return false;
		}

		IEnumerator Wait(float seconds)
		{
			seconds += Time.realtimeSinceStartup;
			for (; Time.realtimeSinceStartup < seconds; )
			{
				yield return null;
			}
		}

		IEnumerator CoAddObstacle(IEnumerable<Vector3> container, Action<Obstacle> onCreate)
		{
			IEnumerator<Vector3> e = container.GetEnumerator();
			List<HalfEdge> polygonBoundingEdges = new List<HalfEdge>();

			if (e.MoveNext())
			{
				Vertex prevVertex = geomManager.CreateVertex(e.Current);
				Vertex firstVertex = prevVertex;

				for (; e.MoveNext(); )
				{
					Vertex currentVertex = geomManager.CreateVertex(e.Current);
					yield return CoAddConstrainedEdge(polygonBoundingEdges, prevVertex, currentVertex);

					prevVertex = currentVertex;
				}

				yield return CoAddConstrainedEdge(polygonBoundingEdges, prevVertex, firstVertex);
			}

			Obstacle obstacle = geomManager.CreateObstacle(polygonBoundingEdges);
			MarkObstacle(obstacle);

			if (onCreate != null)
			{
				onCreate(obstacle);
			}
		}

		IEnumerator CoAddConstrainedEdge(List<HalfEdge> container, Vertex src, Vertex dest)
		{
			if (Append(src))
			{
				yield return Wait(Transition);
			}

			if (Append(dest))
			{
				yield return Wait(Transition);
			}

			for (; src != dest; )
			{
				container.Add(AddConstrainedEdgeAt(ref src, dest));
				yield return Wait(Transition);
			}
		}
	}
}
