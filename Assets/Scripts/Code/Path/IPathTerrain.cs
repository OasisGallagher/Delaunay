using UnityEngine;

namespace Delaunay
{
	public interface IPathTerrain
	{
		float GetTerrainHeight(Vector3 position);
	}
}
