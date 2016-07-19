using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Delaunay
{
	public class Stage : MonoBehaviour
	{
		public DelaunayMesh delaunayMesh;

		public float Width { get { return delaunayMesh.Width; } }
		public float Height { get { return delaunayMesh.Height; } }

		public Vector3 Origin { get { return delaunayMesh.Origin; } }

		public Vector3 PhysicsHeightTest(Vector3 point)
		{
			point.y = 25f;
			RaycastHit hit;
			if (!Physics.Raycast(point, Vector3.down, out hit, 100f))
			{
				Debug.LogError("Raycast failed");
				return point;
			}

			return hit.point;
		}

		void Start()
		{
			delaunayMesh = new DelaunayMesh(new Vector3(-10, 0, -10), 20f, 20f);

			string dm = Path.Combine(EditorConstants.kOutputFolder, "delaunay.dm").Replace('\\', '/');
			print("Load dm file: " + dm);

			delaunayMesh.Load(Path.Combine(EditorConstants.kOutputFolder, "delaunay.dm"));

			gameObject.AddComponent<StressTest>();
		}

		void OnDisable()
		{
			delaunayMesh.ClearAll();
		}

		void OnGUI()
		{
			GUILayout.BeginArea(new Rect(10, 10, 68, 30));
			GUILayout.BeginVertical("Box");

			if (GUILayout.Button("Load", GUILayout.Width(60)))
			{
				string path = UnityEditor.EditorUtility.OpenFilePanel("", EditorConstants.kOutputFolder, "dm");
				if (!string.IsNullOrEmpty(path))
				{
					delaunayMesh.Load(path);
					print(path + " loaded.");
				}
			}

			GUILayout.EndVertical();
			GUILayout.EndArea();
		}

		bool MousePositionToStage(out Vector3 point)
		{
			RaycastHit hit;
			point = Vector3.zero;
			if (!Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 100))
			{
				Debug.LogError("Raycast failed");
				return false;
			}

			point = hit.point;

			return true;
		}
	}
}
