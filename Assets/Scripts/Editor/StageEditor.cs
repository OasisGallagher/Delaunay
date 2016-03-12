using UnityEditor;
using UnityEngine;

namespace Delaunay
{
	[CustomEditor(typeof(Stage))]
	public class StageEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			foreach (Obstacle obstacle in GeomManager.AllObstacles)
			{
				bool active = GUILayout.Toggle(obstacle.__tmpActive, "Obstacle " + obstacle.ID);
				if (active != obstacle.__tmpActive)
				{
					obstacle.__tmpActive = active;
					if (!obstacle.__tmpActive)
					{
						(target as Stage).__tmpRemoveObstacle(obstacle.ID);
					}
					//obstacle.Mesh.ForEach(item => { item.gameObject.SetActive(active); });
				}
			}
		}

		void OnSceneGUI()
		{
		}
	}
}