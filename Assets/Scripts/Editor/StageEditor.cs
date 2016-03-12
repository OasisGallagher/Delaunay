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

		void OnEnable()
		{
			UnityEngine.Debug.Log("EditorGUI.........");
		}

		void OnSceneGUI()
		{
			GeomManager.AllTriangles.ForEach(facet =>
			{
				if (!facet.gameObject.activeSelf) { return; }

				facet.BoundingEdges.ForEach(edge =>
				{
					Vector3 offset = EditorConstants.kEdgeGizmosOffset;

					Color oldColor = Handles.color;
					Handles.color = (edge.Constraint || edge.Pair.Constraint) ? Color.red : Color.white;
					Handles.DrawLine(edge.Src.Position + offset, edge.Dest.Position + offset);
					Handles.color = oldColor;

					Color triangleColor = facet.Walkable ? Color.gray : Color.red;
					Handles.DrawSolidRectangleWithOutline(new Vector3[] 
					{
						facet.A.Position, facet.B.Position, facet.C.Position, facet.A.Position
					}, triangleColor, Color.black);
				});
			});
		}
	}
}
