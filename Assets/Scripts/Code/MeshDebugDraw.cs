using System.Collections.Generic;
using UnityEngine;

namespace Delaunay
{
	public enum DebugDrawMask
	{
		DebugDrawTriangles = 1,
		DebugDrawEdges = 2,
		DebugDrawTiles = 4,
		DebugDrawSuperBorder = 8,
	}

	[RequireComponent(typeof(Camera))]
	public class MeshDebugDraw : MonoBehaviour
	{
		public Vector3 offset = Vector3.up;

		public Color blockFaceColor = new Color(1, 0, 0, 90 / 255f);
		public Color walkableFaceColor = new Color(128 / 255f, 128 / 255f, 128 / 255f, 11 / 255f);
		public Color edgeColor = new Color(0, 205 / 255f, 1, 126 / 255f);

		public Color freeTileFaceColor = new Color(77 / 255f, 64 / 255f, 176 / 255f, 11 / 255f);
		public Color usedTileFaceColor = new Color(159 / 255f, 53 / 255f, 53 / 255f, 11 / 255f);
		public Color tileEdgeColor = new Color(0, 0, 1, 22 / 255f);

		public DebugDrawMask drawMask = (DebugDrawMask)(-1);

		const float kShrink = 0.1f;

		Material simpleMaterial;

		void Awake()
		{
			simpleMaterial = new Material(
				"Shader \"Lines/Colored Blended\" {"
				+ "SubShader { Pass { "
				+ "	BindChannels { Bind \"Color\",color } "
				+ "	Blend SrcAlpha OneMinusSrcAlpha "
				+ "	ZWrite Off Cull Off Fog { Mode Off } "
				+ "} } }");

			simpleMaterial.hideFlags = HideFlags.HideAndDontSave;
			simpleMaterial.shader.hideFlags = HideFlags.HideAndDontSave;
		}

		void OnPostRender()
		{
			DelaunayMesh targetMesh = GameObject.FindObjectOfType<Stage>().delaunayMesh;
			if (targetMesh == null) { return; }

			simpleMaterial.SetPass(0);

			if ((drawMask & DebugDrawMask.DebugDrawTriangles) != 0)
			{
				GL.Begin(GL.TRIANGLES);
				Vector3[] shinkedTriangle = new Vector3[3];
				targetMesh.AllTriangles.ForEach(face =>
				{
					if (face.gameObject.activeSelf)
					{
						shinkedTriangle[0] = face.A.Position;
						shinkedTriangle[1] = face.B.Position;
						shinkedTriangle[2] = face.C.Position;
						MathUtility.Shink(shinkedTriangle, kShrink);
						GL.Color(face.Walkable ? walkableFaceColor : blockFaceColor);
						GL.Vertex(shinkedTriangle[0] + offset);
						GL.Vertex(shinkedTriangle[1] + offset);
						GL.Vertex(shinkedTriangle[2] + offset);
					}
				});

				GL.End();
			}

			if ((drawMask & DebugDrawMask.DebugDrawEdges) != 0)
			{
				GL.Begin(GL.LINES);
				GL.Color(edgeColor);
				targetMesh.AllEdges.ForEach(edge =>
				{
					bool forward = edge.Src.Position.compare2(edge.Dest.Position) < 0;
					if (forward)
					{
						GL.Vertex(edge.Src.Position + offset);
						GL.Vertex(edge.Dest.Position + offset);
					}
				});

				GL.End();
			}

			if ((drawMask & DebugDrawMask.DebugDrawTiles) != 0)
			{
				TiledMap map = targetMesh.Map;
				float width = (map.ColumnCount * map.TileSize);
				float height = (map.RowCount * map.TileSize);

				GL.Begin(GL.QUADS);
				for (int i = 0; i < map.RowCount; ++i)
				{
					for (int j = 0; j < map.ColumnCount; ++j)
					{
						Tile tile = map[i, j];
						GL.Color(tile.Face != null ? usedTileFaceColor : freeTileFaceColor);
						Vector3 center = map.GetTileCenter(i, j) + offset;
						Vector3 deltaX = new Vector3(map.TileSize / 2f, 0, 0);
						Vector3 deltaZ = new Vector3(0, 0, map.TileSize / 2f);
						GL.Vertex(center - deltaX - deltaZ);
						GL.Vertex(center - deltaX + deltaZ);
						GL.Vertex(center + deltaX + deltaZ);
						GL.Vertex(center + deltaX - deltaZ);
					}
				}
				GL.End();

				GL.Begin(GL.LINES);
				GL.Color(tileEdgeColor);

				for (int i = 0; i < map.RowCount + 1; ++i)
				{
					Vector3 start = map.Origin + i * map.TileSize * Vector3.forward + offset;
					GL.Vertex(start);
					GL.Vertex(start + width * Vector3.right);
				}

				for (int i = 0; i < map.ColumnCount + 1; ++i)
				{
					Vector3 start = map.Origin + i * map.TileSize * Vector3.right + offset;
					GL.Vertex(start);
					GL.Vertex(start + height * Vector3.forward);
				}

				GL.End();
			}

			if ((drawMask & DebugDrawMask.DebugDrawSuperBorder) != 0)
			{
				Vector3? first = null;

				GL.Begin(GL.LINES);
				GL.Color(Color.red);

				for (IEnumerator<Vector3> e = targetMesh.BorderVertices.GetEnumerator(); e.MoveNext(); )
				{
					first = first ?? e.Current;
					GL.Vertex(e.Current + offset);
				}
				
				if (first.HasValue)
				{
					GL.Vertex(first.Value);
				}

				GL.End();
			}
		}
	}
}

