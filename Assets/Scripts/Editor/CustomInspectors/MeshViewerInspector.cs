using UnityEngine;
using UnityEditor;

namespace Delaunay
{
	[CustomEditor(typeof(MeshViewer))]
	public class MeshViewerInspector : Editor
	{
		public override void OnInspectorGUI()
		{
			MeshViewer meshViewer = target as MeshViewer;
			EditorGUILayout.BeginVertical("Box");
			meshViewer.viewerMask = (MeshViewerMask)EditorGUILayout.EnumMaskField("Viewer mask", meshViewer.viewerMask);
			meshViewer.offset = EditorGUILayout.Vector3Field("Offset", meshViewer.offset);
			meshViewer.blockFaceColor = EditorGUILayout.ColorField("Block face color", meshViewer.blockFaceColor);
			meshViewer.walkableFaceColor = EditorGUILayout.ColorField("Walkable face color", meshViewer.walkableFaceColor);
			meshViewer.edgeColor = EditorGUILayout.ColorField("Edge color", meshViewer.edgeColor);
			meshViewer.freeTileFaceColor = EditorGUILayout.ColorField("Free tile face color", meshViewer.freeTileFaceColor);
			meshViewer.usedTileFaceColor = EditorGUILayout.ColorField("Used tile face color", meshViewer.usedTileFaceColor);
			meshViewer.tileEdgeColor = EditorGUILayout.ColorField("Tile edge color", meshViewer.tileEdgeColor);
			EditorGUILayout.EndVertical();
		}
	}
}