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

			EditorGUILayout.BeginHorizontal("Box");
			triangle.A.Position.y = EditorGUILayout.Slider("A:" + triangle.A.Position, triangle.A.Position.y, -1000, 1000);
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal("Box");
			triangle.B.Position.y = EditorGUILayout.Slider("B:" + triangle.B.Position, triangle.B.Position.y, -1000, 1000);
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal("Box");
			triangle.C.Position.y = EditorGUILayout.Slider("C:" + triangle.C.Position, triangle.C.Position.y, -1000, 1000);
			EditorGUILayout.EndHorizontal();
		}

		public void OnSceneGUI()
		{
		}
	}
}
