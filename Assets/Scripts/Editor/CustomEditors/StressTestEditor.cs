using UnityEngine;
using UnityEditor;

namespace Delaunay
{
	[CustomEditor(typeof(StressTest))]
	public class StressTestEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			StressTest stressTest = target as StressTest;
			EditorGUILayout.BeginVertical("Box");
			stressTest.PlayerCount = EditorGUILayout.IntField("Player count", stressTest.PlayerCount);
			EditorGUILayout.EndVertical();
		}
	}
}
