using UnityEditor;
using UnityEngine;

namespace Delaunay
{
	[CustomEditor(typeof(Triangle))]
	public class TriangleEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			Triangle triangle = target as Triangle;
			triangle.Walkable = GUILayout.Toggle(triangle.Walkable, "Walkable");
			GUILayout.Label("A:" + triangle.A);
			GUILayout.Label("B:" + triangle.B);
			GUILayout.Label("C:" + triangle.C);
		}

		public void OnSceneGUI()
		{
			Triangle triangle = target as Triangle;
			Vector3 position = triangle.A.Position;
			position = Handles.DoPositionHandle(position, Quaternion.identity);
			if (position.y != triangle.A.Position.y)
			{
				triangle.A.Position.y = position.y;
			}

			position = triangle.B.Position;
			position = Handles.DoPositionHandle(position, Quaternion.identity);
			if (position.y != triangle.B.Position.y)
			{
				triangle.B.Position.y = position.y;
			}

			position = triangle.C.Position;
			
			position = Handles.DoPositionHandle(position, Quaternion.identity);
			if (position.y != triangle.C.Position.y)
			{
				triangle.C.Position.y = position.y;
			}
		}
	}
}
