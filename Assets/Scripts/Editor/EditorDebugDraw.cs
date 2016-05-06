using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Delaunay
{
	public class EditorDebugDraw
	{
		DelaunayMesh mesh;
		public EditorDebugDraw(DelaunayMesh mesh)
		{
			this.mesh = mesh;
		}

		DebugDrawMask drawMask = (DebugDrawMask)(-1);
		Color blockFaceColor = new Color(1, 0, 0, 90 / 255f);
		Color walkableFaceColor = new Color(128 / 255f, 128 / 255f, 128 / 255f, 11 / 255f);
		Color edgeColor = new Color(0, 205 / 255f, 1, 126 / 255f);

		Color freeTileFaceColor = new Color(77 / 255f, 64 / 255f, 176 / 255f, 11/255f);
		Color usedTileFaceColor = new Color(159 / 255f, 53 / 255f, 53 / 255f, 11 / 255f);
		Color tileEdgeColor = new Color(0, 0, 1, 22 / 255f);

		public void OnGUI()
		{
			EditorGUILayout.BeginVertical("Box");
			drawMask = (DebugDrawMask)EditorGUILayout.EnumMaskField("Draw mask", drawMask);
			blockFaceColor = EditorGUILayout.ColorField("Block face color", blockFaceColor);
			walkableFaceColor = EditorGUILayout.ColorField("Walkable face color", walkableFaceColor);
			edgeColor = EditorGUILayout.ColorField("Edge color", edgeColor);
			freeTileFaceColor = EditorGUILayout.ColorField("Free tile face color", freeTileFaceColor);
			usedTileFaceColor = EditorGUILayout.ColorField("Used tile face color", usedTileFaceColor);
			tileEdgeColor = EditorGUILayout.ColorField("Tile edge color", tileEdgeColor);
			EditorGUILayout.EndVertical();
		}

		public void DrawPolyLine(List<Vector3> points, Color color)
		{
			Color oldColor = Handles.color;
			Handles.color = color;
			Handles.DrawPolyLine(points.ToArray());
			foreach (Vector3 element in points)
			{
				Handles.SphereCap(5, element, Quaternion.identity, 0.1f);
			}
			Handles.color = oldColor;
		}

		public void DrawDelaunayMesh()
		{
			if ((drawMask & DebugDrawMask.DebugDrawTriangles) != 0)
			{
				mesh.AllTriangles.ForEach(face =>
				{
					if (!face.gameObject.activeSelf) { return; }

					Color color = face.Walkable ? walkableFaceColor : blockFaceColor;
					Vector3[] verts = new Vector3[]
					{
						face.A.Position + EditorConstants.kMeshOffset,
						face.B.Position + EditorConstants.kMeshOffset,
						face.C.Position + EditorConstants.kMeshOffset,
						face.A.Position + EditorConstants.kMeshOffset,
					};

					Handles.DrawSolidRectangleWithOutline(verts, color, color);
				});
			}

			if ((drawMask & DebugDrawMask.DebugDrawEdges) != 0)
			{
				Color handlesOldColor = Handles.color;
				
				mesh.AllEdges.ForEach(edge =>
				{
					Handles.color = (edge.Constraint || edge.Pair.Constraint) ? blockFaceColor : edgeColor;
					bool forward = edge.Src.Position.compare2(edge.Dest.Position) < 0;
					if (forward)
					{
						Handles.DrawLine(edge.Src.Position + EditorConstants.kMeshOffset,
							edge.Dest.Position + EditorConstants.kMeshOffset
						);
					}
				});

				Handles.color = handlesOldColor;
			}

			if ((drawMask & DebugDrawMask.DebugDrawTiles) != 0)
			{
				TiledMap map = mesh.Map;

				for (int i = 0; i < map.RowCount; ++i)
				{
					for (int j = 0; j < map.ColumnCount; ++j)
					{
						Tile tile = map[i, j];
						Vector3 center = map.GetTileCenter(i, j) + EditorConstants.kMeshOffset;
						Vector3 deltaX = new Vector3(map.TileSize / 2f, 0, 0);
						Vector3 deltaZ = new Vector3(0, 0, map.TileSize / 2f);
						Vector3[] verts = new Vector3[]
						{
							center - deltaX - deltaZ,
							center - deltaX + deltaZ,
							center + deltaX + deltaZ,
							center + deltaX - deltaZ
						};

						Handles.DrawSolidRectangleWithOutline(verts, tile.Face != null ? usedTileFaceColor : freeTileFaceColor, tileEdgeColor);
					}
				}
			}

			if ((drawMask & DebugDrawMask.DebugDrawSuperBorder) != 0)
			{
				IEnumerator<Vector3> e = mesh.BorderVertices.GetEnumerator();
				Vector3 prev = Vector3.zero, first = Vector3.zero;
				Color oldColor = Handles.color;
				Handles.color = Color.red;

				if (e.MoveNext())
				{
					first = prev = e.Current;
					for (; e.MoveNext(); )
					{
						Handles.DrawLine(prev + EditorConstants.kMeshOffset, e.Current + EditorConstants.kMeshOffset);
						prev = e.Current;
					}

					Handles.DrawLine(prev + EditorConstants.kMeshOffset, first + EditorConstants.kMeshOffset);
				}

				Handles.color = oldColor;
			}
		}
	}
}
