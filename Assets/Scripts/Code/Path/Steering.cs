using System.Collections.Generic;
using UnityEngine;

namespace Delaunay
{
	[RequireComponent(typeof(Pathway))]
	public class Steering : MonoBehaviour
	{
		PlayerComponent playerComponent;
		float distance = 0f;
		IPathTerrain terrain = null;
		Pathway pathway = null;

		void Start()
		{
			pathway = GetComponent<Pathway>();
			playerComponent = GetComponent<PlayerComponent>();
		}

		public void SetPath(List<Vector3> value)
		{
			pathway.Points = value != null ? value.ToArray() : null;
			distance = 0f;
		}

		public void SetTerrain(IPathTerrain terrain)
		{
			this.terrain = terrain;
		}

		void Update()
		{
			if (distance < pathway.Length)
			{
				Vector3 newPosition = pathway.DistanceToPoint(distance += playerComponent.Speed * Time.deltaTime);
				transform.position = new Vector3(newPosition.x, terrain.GetTerrainHeight(newPosition), newPosition.z);
			}
		}
	}
}
