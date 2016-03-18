using UnityEditor;
using UnityEngine;

namespace Delaunay
{
	public class DebugDraw
	{
		enum DebugDrawMask
		{
			DebugDrawTriangles = 1,
			DebugDrawEdges = 2,
			DebugDrawTiles = 4,
		}

		DebugDrawMask drawMask;
		Color blockFaceColor = Color.red;
		Color walkableFaceColor = Color.gray;
		Color edgeColor = Color.black;

		Color freeTileFaceColor = new Color(77 / 255f, 64 / 255f, 176 / 255f, 1f);
		Color usedTileFaceColor = new Color(159 / 255f, 53 / 255f, 53 / 255f, 22 / 255f);
		Color tileEdgeColor = new Color(4 / 255f, 4 / 255f, 4 / 255f, 76 / 255f);

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

		public void Draw()
		{
			if ((drawMask & DebugDrawMask.DebugDrawTriangles) != 0)
			{
				GeomManager.AllTriangles.ForEach(face =>
				{
					if (face.gameObject.activeSelf)
					{
						Color color = face.Walkable ? walkableFaceColor : blockFaceColor;
						Vector3[] verts = new Vector3[]
						{
							face.A.Position + EditorConstants.kTriangleMeshOffset,
							face.B.Position + EditorConstants.kTriangleMeshOffset,
							face.C.Position + EditorConstants.kTriangleMeshOffset,
							face.A.Position + EditorConstants.kTriangleMeshOffset,
						};

						Handles.DrawSolidRectangleWithOutline(verts, color, color);
					}
				});
			}

			if ((drawMask & DebugDrawMask.DebugDrawEdges) != 0)
			{
				Color handlesOldColor = Handles.color;
				Handles.color = edgeColor;
				GeomManager.AllEdges.ForEach(edge =>
				{
					bool forward = edge.Src.Position.compare2(edge.Dest.Position) < 0;
					if (forward)
					{
						Handles.DrawLine(edge.Src.Position + EditorConstants.kTriangleMeshOffset,
							edge.Dest.Position + EditorConstants.kTriangleMeshOffset
						);
					}
				});

				Handles.color = handlesOldColor;
			}

			if ((drawMask & DebugDrawMask.DebugDrawTiles) != 0)
			{
				TiledMap map = GeomManager.Map;

				for (int i = 0; i < map.RowCount; ++i)
				{
					for (int j = 0; j < map.ColumnCount; ++j)
					{
						Tile tile = map[i, j];
						Vector3 center = map.GetTileCenter(i, j) + EditorConstants.kTriangleMeshOffset;
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
		}
	}
}
