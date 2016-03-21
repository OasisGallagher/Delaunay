using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Delaunay
{
	public class SceneEditor : EditorWindow
	{
		DebugDraw debugDraw;
		bool planting = false;

		List<Vector3> plantedVertices;

		DelaunayMesh delaunayMesh;

		[MenuItem("Window/Scene Editor")]
		static void OpenEditor()
		{
			EditorWindow.GetWindow<SceneEditor>(false, "Scene Editor");
		}

		void OnEnable()
		{
			SceneView.onSceneGUIDelegate += OnUpdateSceneGUI;
			debugDraw = new DebugDraw();
			plantedVertices = new List<Vector3>();

			GameObject floor = GameObject.Find("Floor");

			Vector3 scale = floor.transform.localScale / 2f;
			const float padding = 0.2f;

			Vector3 floorPosition = floor.transform.position;
			Rect rect = new Rect(floorPosition.x - scale.x - padding, floorPosition.z - scale.z - padding, (scale.x + padding) * 2, (scale.z + padding) * 2);

			List<Vector3> borderCorners = new List<Vector3>
			{
				new Vector3(rect.xMax, 0, rect.yMax),
				new Vector3(rect.xMin, 0, rect.yMax),
				new Vector3(rect.xMin, 0, rect.yMin),
				new Vector3(rect.xMax, 0, rect.yMin)
			};

			delaunayMesh = new DelaunayMesh(borderCorners);
		}

		void OnDisable()
		{
			plantedVertices.Clear();

			if (delaunayMesh != null)
			{
				delaunayMesh.Clear();
				delaunayMesh = null;
			}

			SceneView.onSceneGUIDelegate -= OnUpdateSceneGUI;
		}

		void OnUpdateSceneGUI(SceneView sceneView)
		{
			if (debugDraw != null)
			{
				debugDraw.DrawDelaunayMesh();
				debugDraw.DrawPolyLine(plantedVertices);
			}

			DrawSceneGUI();
			OnInput();
		}

		void DrawSceneGUI()
		{
			DrawStats();
			DrawCommands();
		}

		private void DrawCommands()
		{
			GUILayout.BeginArea(new Rect(10, 10, 90, 120));

			GUILayout.BeginVertical("Box", GUILayout.Width(EditorConstants.kPanelWidth));

			if (GUILayout.Button("Save"))
			{
				SaveMesh();
			}

			if (GUILayout.Button("Load"))
			{
				LoadMesh();
			}

			if (GUILayout.Button("Clear"))
			{
				ClearMesh();
			}

			GUILayout.EndVertical();
			GUILayout.EndArea();
		}

		void LoadMesh()
		{
			string path = UnityEditor.EditorUtility.OpenFilePanel("", Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "xml");
			if (!string.IsNullOrEmpty(path))
			{
				ClearMesh();
				SerializeTools.Load(path);
				ShowNotification(new GUIContent(Path.GetFileName(path) + " loaded."));
			}
		}

		void SaveMesh()
		{
			string path = UnityEditor.EditorUtility.SaveFilePanel("", Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "delaunay", "xml");
			if (!string.IsNullOrEmpty(path))
			{
				SerializeTools.Save(path);
			}
		}

		void ClearMesh()
		{
			if (delaunayMesh != null) { delaunayMesh.Clear(); }
		}

		void DrawStats()
		{
			GUILayout.BeginArea(new Rect(Screen.width - 80, Screen.height - 100, 90, 100));

			GUILayout.BeginVertical("Box");
			GUILayout.Label("V: " + GeomManager.AllVertices.Count);
			GUILayout.Label("E: " + GeomManager.AllEdges.Count);
			GUILayout.Label("T: " + GeomManager.AllTriangles.Count);
			GUILayout.EndVertical();

			GUILayout.EndArea();
		}

		void OnUpdate()
		{ 
		}

		void OnGUI()
		{
			if (debugDraw != null)
			{
				debugDraw.OnGUI();
			}

			if (GUILayout.Button(planting ? "Cancel" : "Plant"))
			{
				OnClickPlant();
			}
		}

		void OnClickPlant()
		{
			planting = !planting;
			plantedVertices.Clear();
		}

		bool CheckNewVertex(Vector3 point)
		{
			if (plantedVertices.Count == 0) { return true; }
			Vector3 cross = Vector3.zero;
			for (int i = 1; i < plantedVertices.Count; ++i)
			{
				CrossState state = MathUtility.GetLineCrossPoint(out cross, plantedVertices[i],
					plantedVertices[i - 1], point, plantedVertices.back());

				if (state == CrossState.CrossOnSegment && !cross.equals2(plantedVertices.back()))
				{
					return false;
				}
			}

			return true;
		}

		void OnInput()
		{
			if (Event.current.type == EventType.KeyUp)
			{
				OnKeyboardInput(Event.current.keyCode);
			}
		}

		void OnKeyboardInput(KeyCode keyCode)
		{
			if (planting && keyCode == KeyCode.BackQuote)
			{
				Vector3 point = FixedMousePosition;
				if (!CheckNewVertex(point))
				{
					Debug.LogError("Invalid point " + point);
				}
				else
				{
					plantedVertices.Add(point);
				}
			}

			if (planting && keyCode == KeyCode.Return && plantedVertices.Count > 0)
			{
				delaunayMesh.AddObstacle(plantedVertices);
				plantedVertices.Clear();
			}
		}

		Vector3 FixedMousePosition
		{
			get
			{
				Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
				RaycastHit hit;
				if (!Physics.Raycast(ray, out hit, 100, -1))
				{
					Debug.LogError("Raycast failed");
				}

				return hit.point;
			}
		}
	}
}
