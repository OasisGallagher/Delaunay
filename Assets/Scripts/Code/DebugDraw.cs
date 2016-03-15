using UnityEngine;

namespace Delaunay
{
	[System.Flags]
	public enum DebugDrawMask
	{
		DebugDrawTriangles = 1,
		DebugDrawEdges = 2,
		DebugDrawTiles = 3,
	}

	[RequireComponent(typeof(Camera))]
	public class DebugDraw : MonoBehaviour
	{
		[HideInInspector]
		public DebugDrawMask DrawMask;

		public Color BlockFaceColor = Color.red;
		public Color WalkableFaceColor = Color.gray;
		public Color EdgeColor = Color.black;
		public Color TileColor = Color.black;

		public float Shrink = 0.1f;

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
			simpleMaterial.SetPass(0);

			if ((DrawMask & DebugDrawMask.DebugDrawTriangles) != 0)
			{
				DrawTriangles();
			}

			if ((DrawMask & DebugDrawMask.DebugDrawEdges) != 0)
			{
				DrawEdges();
			}

			if ((DrawMask & DebugDrawMask.DebugDrawTiles) != 0)
			{
				DrawTiles();
			}
		}

		void DrawEdges()
		{
			GL.Begin(GL.LINES);
			GL.Color(EdgeColor);
			GeomManager.AllEdges.ForEach(edge =>
			{
				bool forward = edge.Src.Position.compare2(edge.Dest.Position) < 0;
				if (forward)
				{
					GL.Vertex(edge.Src.Position + EditorConstants.kTriangleMeshOffset);
					GL.Vertex(edge.Dest.Position + EditorConstants.kTriangleMeshOffset);
				}
			});

			GL.End();
		}

		void DrawTriangles()
		{
			GL.Begin(GL.TRIANGLES);
			Vector3[] shinkedTriangle = new Vector3[3];
			GeomManager.AllTriangles.ForEach(face =>
			{
				if (face.gameObject.activeSelf)
				{
					shinkedTriangle[0] = face.A.Position;
					shinkedTriangle[1] = face.B.Position;
					shinkedTriangle[2] = face.C.Position;
					MathUtility.Shink(shinkedTriangle, Shrink);
					GL.Color(face.Walkable ? WalkableFaceColor : BlockFaceColor);
					GL.Vertex(shinkedTriangle[0] + EditorConstants.kTriangleMeshOffset);
					GL.Vertex(shinkedTriangle[1] + EditorConstants.kTriangleMeshOffset);
					GL.Vertex(shinkedTriangle[2] + EditorConstants.kTriangleMeshOffset);
				}
			});

			GL.End();
		}

		void DrawTiles()
		{
			TiledMap map = GeomManager.Map;
			float width = (map.ColumnCount * map.TileSize);
			float height = (map.RowCount * map.TileSize);

			GL.Begin(GL.LINES);
			GL.Color(TileColor);

			for (int i = 0; i < map.RowCount + 1; ++i)
			{
				Vector3 start = map.Origin + i * map.TileSize * Vector3.forward + EditorConstants.kTriangleMeshOffset;
				GL.Vertex(start);
				GL.Vertex(start + width * Vector3.right);
			}

			for (int i = 0; i < map.ColumnCount + 1; ++i)
			{
				Vector3 start = map.Origin + i * map.TileSize * Vector3.right + EditorConstants.kTriangleMeshOffset;
				GL.Vertex(start);
				GL.Vertex(start + height * Vector3.forward);
			}

			GL.End();
		}
	}
}
