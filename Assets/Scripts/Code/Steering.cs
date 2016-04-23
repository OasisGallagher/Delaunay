using System.Collections.Generic;
using UnityEngine;

namespace Delaunay
{
	public class Steering : MonoBehaviour
	{
		public float Speed;

		public void SetPath(List<Vector3> value)
		{
			pathway.Points = value != null ? value.ToArray() : null;
			distance = 0f;
		}

		public void SetTerrain(IPathTerrain terrain)
		{
			this.terrain = terrain;
		}

		float distance = 0f;
		IPathTerrain terrain = null;
		Pathway pathway = new Pathway();

		void Update()
		{
			if (distance < pathway.Length)
			{
				Vector3 newPosition = pathway.DistanceToPoint(distance += Speed * Time.deltaTime);
				transform.position = new Vector3(newPosition.x, terrain.GetTerrainHeight(newPosition), newPosition.z);
			}
		}

		void OnDrawGizmos()
		{
			if (pathway.Points == null) { return; }
			Color oldColor = Gizmos.color;
			Gizmos.color = Color.gray;

			for (int i = 1; i < pathway.Points.Length; ++i)
			{
				Gizmos.DrawLine(pathway.Points[i - 1] + EditorConstants.kPathOffset, pathway.Points[i] + EditorConstants.kPathOffset);
			}

			Gizmos.color = oldColor;
		}
	}
}
