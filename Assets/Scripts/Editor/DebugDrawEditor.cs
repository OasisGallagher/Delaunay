using UnityEditor;
using UnityEngine;

namespace Delaunay
{
	[CustomEditor(typeof(DebugDraw))]
	public class DebugDrawEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			DebugDraw component = target as DebugDraw;
			component.DrawMask = (DebugDrawMask)EditorGUILayout.EnumMaskField("Draw Mask", component.DrawMask);
		}
	}
}
