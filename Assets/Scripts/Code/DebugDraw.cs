using UnityEngine;

namespace Delaunay
{
	[RequireComponent(typeof(Camera))]
	public class DebugDraw : MonoBehaviour
	{
		public Color BlockFaceColor = Color.red;
		public Color WalkableFaceColor = Color.gray;
		public Color EdgeColor = Color.black;

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
			GeomManager.AllTriangles.ForEach(face =>
			{
				if (face.gameObject.activeSelf)
				{
					DrawTriangle(face.A.Position, face.B.Position, face.C.Position, face.Walkable ? WalkableFaceColor : BlockFaceColor);
				}
			});
		}

		void DrawTriangle(Vector3 va, Vector3 vb, Vector3 vc, Color faceColor)
		{
			simpleMaterial.SetPass(0);

			GL.Begin(GL.TRIANGLES);
			GL.Color(faceColor);
			GL.Vertex(va);
			GL.Vertex(vb);
			GL.Vertex(vc);
			GL.End();

			GL.Begin(GL.LINES);
			GL.Color(EdgeColor);
			GL.Vertex(va);
			GL.Vertex(vb);
			GL.Vertex(vc);
			GL.Vertex(va);
			GL.End();
		}
	}
}
