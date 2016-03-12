using System.Collections.Generic;
using UnityEngine;

namespace Delaunay
{
	public class Steering : MonoBehaviour
	{
		public float Speed;
		public List<Vector3> Path
		{
			get { return path; }
			set
			{
				path = value;
				currentNode = 0;
				velocity = Vector3.zero;
				lastPosition = transform.position;
				print("Set new path");
			}
		}

		int currentNode = 0;

		Vector3 velocity, lastPosition;
		List<Vector3> path;

		void Update()
		{
			if (path != null && path.Count > 0)
			{
				float s = Time.deltaTime * Speed;
				lastPosition = transform.position;
				transform.position += velocity * s;
				UpdateVelocity();
			}
		}

		void OnDrawGizmos()
		{
			if (path == null) { return; }
			Color oldColor = Gizmos.color;
			Gizmos.color = Color.gray;

			for (int i = 1; i < path.Count; ++i)
			{
				Vector3 prev = path[i - 1];
				Vector3 current = path[i];
				prev.y = current.y = EditorConstants.kConvexHullGizmosHeight;
				Gizmos.DrawLine(prev, current);
			}

			Gizmos.color = oldColor;
		}

		void UpdateVelocity()
		{
			if (currentNode >= path.Count)
			{
				velocity = Vector3.zero;
				return;
			}

			if (transform.position == lastPosition 
				|| transform.position.dot2(path[currentNode], lastPosition) < 0f)
			{
				++currentNode;
				if (currentNode >= path.Count)
				{
					velocity = Vector3.zero;
					print("Finished steering");
				}
				else
				{
					velocity = (path[currentNode] - path[currentNode - 1]).normalized;
				}
			}
		}
	}
}
