using UnityEngine;
using UnityEditor;

namespace Delaunay
{
	[CustomEditor(typeof(PlayerComponent))]
	public class PlayerComponentEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			PlayerComponent player = target as PlayerComponent;
			EditorGUILayout.BeginVertical("Box");
			player.Radius = EditorGUILayout.FloatField("Radius", player.Radius);
			EditorGUILayout.EndVertical();
		}
	}
}
