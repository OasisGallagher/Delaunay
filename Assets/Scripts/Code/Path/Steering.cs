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

		public delegate void PositionChangedDelegate(PlayerComponent player, Vector3 oldPosition, Vector3 newPosition);
		public event PositionChangedDelegate onPositionChanged;

		void Awake()
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
				Vector3 oldPosition = transform.position;
				Vector3 newPosition = pathway.DistanceToPoint(distance += playerComponent.Speed * Time.deltaTime);
				newPosition = new Vector3(newPosition.x, terrain.GetTerrainHeight(newPosition), newPosition.z);
				transform.position = newPosition;

				if (!oldPosition.equals2(newPosition) && onPositionChanged != null)
				{
					onPositionChanged(playerComponent, oldPosition, newPosition);
				}
			}
		}
	}
}
