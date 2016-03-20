using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Delaunay
{
	public class SceneEditor : EditorWindow
	{
		DebugDraw debugDraw;
		bool planting = false;

		List<Vector3> plantedVertices;

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
		}

		void OnDisable()
		{
			plantedVertices.Clear();

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
				//	Clear();
			}

			GUILayout.EndVertical();
			GUILayout.EndArea();
		}

		void LoadMesh()
		{
			string path = UnityEditor.EditorUtility.OpenFilePanel("", Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "xml");
			if (!string.IsNullOrEmpty(path))
			{
				//Clear();
				SerializeTools.Load(path);
				Debug.Log(path + " loaded.");
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
				GameObject.FindObjectOfType<Stage>().AddObstacle(plantedVertices);
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
