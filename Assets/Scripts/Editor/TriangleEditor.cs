using UnityEditor;
using UnityEngine;

namespace Delaunay
{
	[CustomEditor(typeof(Triangle))]
	public class TriangleEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			Triangle triangle = target as Triangle;
			GUILayout.Label("Walkable:" + triangle.Walkable);
			GUILayout.Label("A:" + triangle.A);
			GUILayout.Label("B:" + triangle.B);
			GUILayout.Label("C:" + triangle.C);
		}

		public void OnSceneGUI()
		{
			Triangle triangle = target as Triangle;
			Vertex modifiedVertex = null;
			do
			{
				Vector3 position = triangle.A.Position;
				position = Handles.DoPositionHandle(position, Quaternion.identity);
				if (position.y != triangle.A.Position.y)
				{
					triangle.A.Position.y = position.y;
					modifiedVertex = triangle.A;
					break;
				}

				position = triangle.B.Position;
				position = Handles.DoPositionHandle(position, Quaternion.identity);
				if (position.y != triangle.B.Position.y)
				{
					triangle.B.Position.y = position.y;
					modifiedVertex = triangle.B;
					break;
				}

				position = triangle.C.Position;
				position = Handles.DoPositionHandle(position, Quaternion.identity);
				if (position.y != triangle.C.Position.y)
				{
					triangle.C.Position.y = position.y;
					modifiedVertex = triangle.C;
				}
			} while (false);

			if (modifiedVertex != null)
			{
			}
		}
	}
}
