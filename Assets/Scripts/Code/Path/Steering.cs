using System.Collections.Generic;
using UnityEngine;

namespace Delaunay
{
	/// <summary>
	/// 指定路径, 移动物体.
	/// </summary>
	[RequireComponent(typeof(Pathway))]
	public class Steering : MonoBehaviour
	{
		PlayerComponent playerComponent;

		/// <summary>
		/// 已移动的距离.
		/// </summary>
		float distance = 0f;

		/// <summary>
		/// 地形.
		/// </summary>
		IPathTerrain terrain = null;

		/// <summary>
		/// 路径.
		/// </summary>
		Pathway pathway = null;

		public delegate void PositionChangedDelegate(PlayerComponent player, Vector3 oldPosition, Vector3 newPosition);

		/// <summary>
		/// 物体的位置发生变化时的事件.
		/// </summary>
		public event PositionChangedDelegate onPositionChanged;

		void Awake()
		{
			pathway = GetComponent<Pathway>();
			playerComponent = GetComponent<PlayerComponent>();
		}

		/// <summary>
		/// 设置路径, null表示清空.
		/// </summary>
		public void SetPath(List<Vector3> value)
		{
			pathway.Points = value != null ? value.ToArray() : null;
			distance = 0f;
		}

		/// <summary>
		/// 设置地形.
		/// </summary>
		public void SetTerrain(IPathTerrain terrain)
		{
			this.terrain = terrain;
		}

		void Update()
		{
			// 沿路径移动物体.
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
