using UnityEngine;

namespace Delaunay
{
	[RequireComponent(typeof(Camera))]
	public class DebugDraw : MonoBehaviour
	{
		public Color BlockFaceColor = Color.red;
		public Color WalkableFaceColor = Color.gray;
		public Color EdgeColor = Color.black;
		public float ShrinkAmount = 0.1f;

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

			GL.Begin(GL.TRIANGLES);
			Vector3[] shinkedTriangle = new Vector3[3];
			GeomManager.AllTriangles.ForEach(face =>
			{
				if (face.gameObject.activeSelf)
				{
					shinkedTriangle[0] = face.A.Position;
					shinkedTriangle[1] = face.B.Position;
					shinkedTriangle[2] = face.C.Position;
					MathUtility.Shink(shinkedTriangle, ShrinkAmount);
					GL.Color(face.Walkable ? WalkableFaceColor : BlockFaceColor);
					GL.Vertex(shinkedTriangle[0] + EditorConstants.kTriangleMeshOffset);
					GL.Vertex(shinkedTriangle[1] + EditorConstants.kTriangleMeshOffset);
					GL.Vertex(shinkedTriangle[2] + EditorConstants.kTriangleMeshOffset);
				}
			});

			GL.End();

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
	}
}
