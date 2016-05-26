using UnityEngine;
using UnityEditor;

namespace Delaunay
{
	[CustomEditor(typeof(MeshDebugDraw))]
	public class MeshDebugDrawEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			MeshDebugDraw debugDraw = target as MeshDebugDraw;
			EditorGUILayout.BeginVertical("Box");
			debugDraw.offset = EditorGUILayout.Vector3Field("Offset", debugDraw.offset);
			debugDraw.drawMask = (DebugDrawMask)EditorGUILayout.EnumMaskField("Draw mask", debugDraw.drawMask);
			debugDraw.blockFaceColor = EditorGUILayout.ColorField("Block face color", debugDraw.blockFaceColor);
			debugDraw.walkableFaceColor = EditorGUILayout.ColorField("Walkable face color", debugDraw.walkableFaceColor);
			debugDraw.edgeColor = EditorGUILayout.ColorField("Edge color", debugDraw.edgeColor);
			debugDraw.freeTileFaceColor = EditorGUILayout.ColorField("Free tile face color", debugDraw.freeTileFaceColor);
			debugDraw.usedTileFaceColor = EditorGUILayout.ColorField("Used tile face color", debugDraw.usedTileFaceColor);
			debugDraw.tileEdgeColor = EditorGUILayout.ColorField("Tile edge color", debugDraw.tileEdgeColor);
			EditorGUILayout.EndVertical();
		}
	}
}