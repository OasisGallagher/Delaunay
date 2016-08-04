using UnityEngine;

namespace Delaunay
{
	/// <summary>
	/// 地形.
	/// </summary>
	public interface IPathTerrain
	{
		/// <summary>
		/// 获取position处的高度.
		/// </summary>
		float GetTerrainHeight(Vector3 position);
	}
}
