using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Delaunay
{
	public class SceneEditor : EditorWindow
	{
		const int kMaxVertices = 32;
		const float kTrimStep = 0.1f;

		DebugDraw debugDraw;
		bool planting = false;

		List<Vector3> plantedVertices;
		BitArray editStates;

		DelaunayMesh delaunayMesh;

		[MenuItem("Window/Scene Editor")]
		static void OpenEditor()
		{
			EditorWindow.GetWindow<SceneEditor>(false, "Scene Editor");
		}

		void OnEnable()
		{
			SceneView.onSceneGUIDelegate += OnUpdateSceneGUI;

			plantedVertices = new List<Vector3>(kMaxVertices);
			editStates = new BitArray(kMaxVertices);

			delaunayMesh = new DelaunayMesh();
			debugDraw = new DebugDraw(delaunayMesh);
		}

		void OnDisable()
		{
			ClearPlanted();

			if (delaunayMesh != null)
			{
				delaunayMesh.ClearAll();
			}

			SceneView.onSceneGUIDelegate -= OnUpdateSceneGUI;
		}

		void OnUpdateSceneGUI(SceneView sceneView)
		{
			if (debugDraw != null)
			{
				debugDraw.DrawDelaunayMesh();
				debugDraw.DrawPolyLine(plantedVertices, HasBorder ? Color.blue : Color.red);
			}

			DrawVertexHandles();

			DrawSceneGUI();
			
			OnInput();
		}

		void DrawSceneGUI()
		{
			if (delaunayMesh != null)
			{
				DrawStats();
				DrawCommands();
			}
		}

		void DrawVertexHandles()
		{
			bool repaint = false;

			for (int i = 0; i < editStates.Count; ++i)
			{
				if (!editStates[i]) { continue; }
				Vector3 position = Handles.DoPositionHandle(plantedVertices[i], Quaternion.identity);
				if (position != plantedVertices[i])
				{
					plantedVertices[i] = position;
					repaint = true;
				}
			}

			if (repaint)
			{
				Repaint();
			}
		}

		void DrawCommands()
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

			EditorGUILayout.Separator();

			if (GUILayout.Button("Clear All"))
			{
				ClearAll();
			}

			if (GUILayout.Button("Clear Mesh"))
			{
				ClearMesh();
			}

			GUILayout.EndVertical();
			GUILayout.EndArea();
		}

		void DrawVertexEditor(int index)
		{
			GUILayout.BeginHorizontal("Box");

			editStates[index] = GUILayout.Toggle(editStates[index], (index + 1).ToString());
			
			plantedVertices[index] = EditorGUILayout.Vector3Field("", plantedVertices[index], GUILayout.Height(16));
			
			if (GUILayout.Button("↑", EditorStyles.miniButtonLeft))
			{
				plantedVertices[index] += new Vector3(0, kTrimStep, 0);
			}

			if (GUILayout.Button("↓", EditorStyles.miniButtonRight))
			{
				plantedVertices[index] -= new Vector3(0, kTrimStep, 0);
			}

			GUILayout.EndHorizontal();
		}

		void LoadMesh()
		{
			string path = EditorUtility.OpenFilePanel("", Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "xml");
			if (!string.IsNullOrEmpty(path))
			{
				delaunayMesh.Load(path);
				ShowNotification(new GUIContent(Path.GetFileName(path) + " loaded."));
			}
		}

		void SaveMesh()
		{
			string path = EditorUtility.SaveFilePanel("", Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "delaunay", "xml");
			if (!string.IsNullOrEmpty(path))
			{
				delaunayMesh.Save(path);
			}
		}

		void ClearMesh()
		{
			if (delaunayMesh != null)
			{
				delaunayMesh.ClearMesh();
			}

			ClearPlanted();
		}

		void ClearAll()
		{
			if (delaunayMesh != null)
			{
				delaunayMesh.ClearAll();
			}

			ClearPlanted();
		}

		void DrawStats()
		{
			GUILayout.BeginArea(new Rect(Screen.width - 80, Screen.height - 100, 90, 100));

			GUILayout.BeginVertical("Box");
			GUILayout.Label("V: " + delaunayMesh.AllVertices.Count);
			GUILayout.Label("E: " + delaunayMesh.AllEdges.Count);
			GUILayout.Label("T: " + delaunayMesh.AllTriangles.Count);
			GUILayout.EndVertical();

			GUILayout.EndArea();
		}

		void OnGUI()
		{
			GUILayout.BeginVertical("Box");

			if (debugDraw != null)
			{
				debugDraw.OnGUI();
			}

			GUILayout.BeginHorizontal("Box");
			if (GUILayout.Button(planting ? "Cancel" : "Plant", GUILayout.Width(60)))
			{
				OnClickPlant();
			}

			if (planting)
			{
				Color oldColor = GUI.color;
				GUI.color = Color.green;
				GUILayout.Label("Click \"~\" in scene to add vertex.", EditorStyles.boldLabel);
				GUI.color = oldColor;
			}

			GUILayout.EndHorizontal();

			for (int i = 0; i < plantedVertices.Count; ++i)
			{
				DrawVertexEditor(i);
			}

			GUILayout.EndVertical();

			if (GUI.changed)
			{
				SceneView.RepaintAll();
			}
		}

		void OnClickPlant()
		{
			planting = !planting;
			ClearPlanted();
		}

		bool CheckNewVertex(Vector3 point)
		{
			if (plantedVertices.Count >= kMaxVertices)
			{
				Debug.LogError("Too many vertices");
				return false;
			}

			Vector3 cross = Vector3.zero;
			for (int i = 1; i < plantedVertices.Count; ++i)
			{
				CrossState state = MathUtility.GetLineCrossPoint(out cross, plantedVertices[i],
					plantedVertices[i - 1], point, plantedVertices.back());

				if (state == CrossState.CrossOnSegment && !cross.equals2(plantedVertices.back()))
				{
					Debug.LogError("Invalid point " + point);
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
			bool repaint = false;
			if (keyCode == KeyCode.BackQuote)
			{
				repaint = OnBackQuote();
			}

			if (keyCode == KeyCode.Return)
			{
				repaint = OnReturn();
			}

			if (repaint)
			{
				Repaint();
			}
		}

		bool OnBackQuote()
		{
			if (!planting) { return false; }
			Vector3 point = FixedMousePosition;

			if (CheckNewVertex(point))
			{
				plantedVertices.Add(point);
				return true;
			}

			return false;
		}

		bool OnReturn()
		{
			if (!planting) { return false; }

			if (plantedVertices.Count < 3)
			{
				EditorUtility.DisplayDialog("Error", "Insufficient vertices for a polygon.", "Ok");
				return false;
			}

			RenderVertices(plantedVertices);
			ClearPlanted();

			return true;
		}

		void RenderVertices(IEnumerable<Vector3> vertices)
		{
			if (HasBorder)
			{
				delaunayMesh.AddObstacle(vertices);
			}

			if(!HasBorder && EditorUtility.DisplayDialog("", "Create border with these vertices?", "Yes", "No"))
			{
				delaunayMesh.AddBorder(vertices);
			}
		}

		void ClearPlanted()
		{
			plantedVertices.Clear();
			editStates.SetAll(false);
		}

		Vector3[] CalculateFloorBorderVertices()
		{
			GameObject floor = GameObject.Find("Stage");

			if (floor == null)
			{
				Debug.LogError("Can not find GameObject named \"Stage\"");
				return null;
			}

			Vector3 scale = floor.transform.localScale / 2f;
			const float padding = 0.2f;

			Vector3 floorPosition = floor.transform.position;
			Rect rect = new Rect(floorPosition.x - scale.x - padding, floorPosition.z - scale.z - padding, (scale.x + padding) * 2, (scale.z + padding) * 2);

			return new Vector3[]
			{
				new Vector3(rect.xMax, 0, rect.yMax),
				new Vector3(rect.xMin, 0, rect.yMax),
				new Vector3(rect.xMin, 0, rect.yMin),
				new Vector3(rect.xMax, 0, rect.yMin)
			};
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

		bool HasBorder
		{
			get { return delaunayMesh != null && delaunayMesh.HasBorder; }
		}
	}
}
