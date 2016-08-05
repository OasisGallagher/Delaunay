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
		/// <summary>
		/// 一次最多可"种"的坐标点个数.
		/// </summary>
		const int kMaxVertices = 32;

		EditorMeshViewer editorMeshViewer;

		/// <summary>
		/// 是否开启"种点".
		/// </summary>
		bool planting = false;

		/// <summary>
		/// 已"种"下的坐标点.
		/// </summary>
		List<Vector3> plantedVertices;

		/// <summary>
		/// 是否编辑该节点. 该结构中的第i位表示是否编辑plantedVertices[i].
		/// </summary>
		BitArray editStates;

		/// <summary>
		/// 网格.
		/// </summary>
		DelaunayMesh delaunayMesh;

		/// <summary>
		/// 编辑命令序列.
		/// </summary>
		EditCommandSequence cmdSequence;

		/// <summary>
		/// 是否编辑调试视图.
		/// </summary>
		bool editMeshViewer = true;

		/// <summary>
		/// 是否在play.
		/// </summary>
		bool isPlaying = false;

		Vector2 vertexEditorScrollViewPosition;
		bool vertexFoldout;

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

			delaunayMesh = new DelaunayMesh(new Vector3(-10f, 0, -10f), 20f, 20f);

			editorMeshViewer = new EditorMeshViewer(delaunayMesh);

			cmdSequence = new EditCommandSequence();
		}

		void OnDisable()
		{
			ClearPlanted();

			if (delaunayMesh != null)
			{
				delaunayMesh.ClearAll();
			}

			if (cmdSequence != null)
			{
				cmdSequence.Clear();
			}

			SceneView.onSceneGUIDelegate -= OnUpdateSceneGUI;
		}

		void Update()
		{
			// 在按下"play"时, 做一些清理.
			if (Application.isPlaying != isPlaying)
			{
				isPlaying = Application.isPlaying;
				OnPlay(isPlaying);
			}
		}

		void OnUpdateSceneGUI(SceneView sceneView)
		{
			if (EditorApplication.isPlaying) { return; }

			// 绘制调试视图.
			if (editorMeshViewer != null)
			{
				editorMeshViewer.DrawDelaunayMesh();
				editorMeshViewer.DrawPolyLine(plantedVertices, HasSuperBorder ? Color.blue : Color.red);
			}

			DrawVertexHandles();

			DrawStats();

			DrawCommands();

			OnInput();
		}

		/// <summary>
		/// 绘制已种的顶点的控制器.
		/// </summary>
		void DrawVertexHandles()
		{
			bool repaint = false;

			for (int i = 0; i < editStates.Count; ++i)
			{
				if (!editStates[i]) { continue; }

				// 改变已"种"的点位置.
				Vector3 position = Handles.DoPositionHandle(plantedVertices[i], Quaternion.identity);
				if (position != plantedVertices[i])
				{
					cmdSequence.Push(new MoveVertexCommand(plantedVertices, i, position));
					repaint = true;
				}
			}

			if (repaint)
			{
				Repaint();
			}
		}

		/// <summary>
		/// 绘制编辑命令.
		/// </summary>
		void DrawCommands()
		{
			if (delaunayMesh == null) { return; }

			GUILayout.BeginArea(new Rect(10, 10, 250, 30));
			GUILayout.BeginHorizontal("Box");

			DrawEditCommand();

			EditorGUILayout.Space();

			DrawSerializeCommand();

			GUILayout.EndHorizontal();
			GUILayout.EndArea();
		}

		/// <summary>
		/// 绘制序列化命令.
		/// </summary>
		void DrawSerializeCommand()
		{
			if (GUILayout.Button("Load", EditorStyles.miniButtonLeft))
			{
				LoadMesh();
			}

			if (GUILayout.Button("Save", EditorStyles.miniButtonMid))
			{
				SaveMesh();
			}

			if (GUILayout.Button("Clear", EditorStyles.miniButtonRight))
			{
				int ans  = 1;
				if (delaunayMesh.HasSuperBorder)
				{
					ans = EditorUtility.DisplayDialogComplex("What do you want to do?",
						"Please choose one of the following options.",
						"Clear mesh", "Clear all", "Cancel"
					);
				}

				if (ans == 0) ClearMesh();
				else if (ans == 1) ClearAll();
			}
		}

		/// <summary>
		/// 绘制编辑命令.
		/// </summary>
		void DrawEditCommand()
		{
			Color oldColor = GUI.backgroundColor;
			GUI.backgroundColor = planting ? Color.green : Color.red;

			// 是否开始"种"坐标点.
			if (GUILayout.Button(planting ? "On" : "Off", EditorStyles.miniButtonLeft))
			{
				planting = !planting;
				ClearPlanted();
			}
			GUI.backgroundColor = oldColor;

			GUI.enabled = cmdSequence != null && cmdSequence.CanUndo;
			if (GUILayout.Button("Undo", EditorStyles.miniButtonMid))
			{
				cmdSequence.Undo();
				Repaint();
			}

			GUI.enabled = cmdSequence != null && cmdSequence.CanRedo;

			if (GUILayout.Button("Redo", EditorStyles.miniButtonRight))
			{
				cmdSequence.Redo();
				Repaint();
			}

			GUI.enabled = true;
		}

		void DrawVertexEditor()
		{
			if (plantedVertices.Count == 0) { return; }

			GUILayout.BeginVertical("Box");
			vertexFoldout = EditorGUILayout.Foldout(vertexFoldout, "Vertices");
			if (vertexFoldout)
			{
				vertexEditorScrollViewPosition = GUILayout.BeginScrollView(vertexEditorScrollViewPosition);
				for (int i = 0; i < plantedVertices.Count; ++i)
				{
					DrawVertexEditorAt(i);
				}

				GUILayout.EndScrollView();
			}

			GUILayout.EndVertical();
		}

		/// <summary>
		/// 编辑plantedVertices[index].
		/// </summary>
		void DrawVertexEditorAt(int index)
		{
			GUILayout.BeginHorizontal("Box");

			editStates[index] = GUILayout.Toggle(editStates[index], (index + 1).ToString());
			
			plantedVertices[index] = EditorGUILayout.Vector3Field("", plantedVertices[index], GUILayout.Height(16));

			GUILayout.EndHorizontal();
		}

		/// <summary>
		/// 加载网格.
		/// </summary>
		void LoadMesh()
		{
			string path = EditorUtility.OpenFilePanel("", EditorConstants.kOutputFolder, "dm");
			if (!string.IsNullOrEmpty(path))
			{
				delaunayMesh.Load(path);

				ClearPlanted();
				cmdSequence.Clear();
				SceneView.currentDrawingSceneView.ShowNotification(new GUIContent(Path.GetFileName(path) + " loaded."));
			}
		}

		/// <summary>
		/// 保存网格.
		/// </summary>
		void SaveMesh()
		{
			string path = EditorUtility.SaveFilePanel("", EditorConstants.kOutputFolder, "delaunay", "dm");
			if (!string.IsNullOrEmpty(path))
			{
				delaunayMesh.Save(path);
			}
		}

		/// <summary>
		/// 清除网格.
		/// </summary>
		void ClearMesh()
		{
			if (delaunayMesh != null)
			{
				delaunayMesh.ClearMesh();
			}

			ClearPlanted();
			cmdSequence.Clear();
		}

		/// <summary>
		/// 清除所有.
		/// </summary>
		void ClearAll()
		{
			if (delaunayMesh != null)
			{
				delaunayMesh.ClearAll();
			}

			ClearPlanted();
			cmdSequence.Clear();
		}

		/// <summary>
		/// 绘制多边形情况.
		/// </summary>
		void DrawStats()
		{
			if (delaunayMesh == null) { return; }

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
			if (EditorApplication.isPlaying) { return; }

			GUILayout.BeginVertical("Box");
			editMeshViewer = EditorGUILayout.Foldout(editMeshViewer, "MeshViewer editor");
			if (editMeshViewer && editorMeshViewer != null)
			{
				editorMeshViewer.OnGUI();
			}
			GUILayout.EndVertical();

			GUILayout.BeginVertical("Box");

			DrawVertexEditor();
			
			GUILayout.EndVertical();

			if (GUI.changed)
			{
				SceneView.RepaintAll();
			}
		}

		void OnPlay(bool play)
		{
			ClearAll();
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

		/// <summary>
		/// 处理输入.
		/// </summary>
		void OnInput()
		{
			if (Event.current.type == EventType.KeyUp)
			{
				OnKeyboardInput(Event.current.keyCode, Event.current.modifiers);
			}
		}

		/// <summary>
		/// 处理键盘输入.
		/// </summary>
		void OnKeyboardInput(KeyCode keyCode, EventModifiers modifiers)
		{
			bool repaint = false;
			if (keyCode == KeyCode.BackQuote)
			{
				repaint = OnBackQuote((Event.current.modifiers & EventModifiers.Shift) != 0);
			}

			if (keyCode == KeyCode.Return)
			{
				repaint = OnReturn(modifiers);
			}

			if (repaint)
			{
				Repaint();
			}
		}

		bool OnBackQuote(bool shift)
		{
			if (!planting) { return false; }
			Vector3 point = FixedMousePosition;

			if (shift && plantedVertices.Count > 0)
			{
				NormalizeMousePosition(ref point, plantedVertices.back());
			}

			if (CheckNewVertex(point))
			{
				cmdSequence.Push(new AddVertexCommand(plantedVertices, point));
				return true;
			}

			return false;
		}

		void NormalizeMousePosition(ref Vector3 position, Vector3 prev)
		{
			float radian = (float)Math.Atan2((double)position.z - prev.z, (double)position.x - prev.x);
			radian += Mathf.PI;

			const float quater = Mathf.PI / 4;

			if ((radian >= quater && radian < 3 * quater) || (radian > 5 * quater && radian < 7 * quater))
			{
				position.x = prev.x;
			}
			else
			{
				position.z = prev.z;
			}
		}

		bool OnReturn(EventModifiers modifiers)
		{
			if (!planting) { return false; }

			if (plantedVertices.Count < 3)
			{
				EditorUtility.DisplayDialog("Error", "Insufficient vertices for a polygon.", "Ok");
				return false;
			}

			RenderVertices(plantedVertices, (modifiers & EventModifiers.Shift) != 0);
			ClearPlanted();

			return true;
		}

		void RenderVertices(List<Vector3> vertices, bool shift)
		{
			if (HasSuperBorder)
			{
				CreateObject(vertices, shift);
			}
			else if (EditorUtility.DisplayDialog("", "Create super border with these vertices?", "Yes", "No"))
			{
				cmdSequence.Push(new CreateSuperBorderCommand(vertices, delaunayMesh));
			}
		}

		void CreateObject(List<Vector3> vertices, bool shift)
		{
			if (shift)
			{
				bool close = EditorUtility.DisplayDialog("", "Close border set?", "Yes", "No");
				cmdSequence.Push(new CreateBorderSetCommand(vertices, delaunayMesh, close));
			}
			else
			{
				cmdSequence.Push(new CreateObstacleCommand(vertices, delaunayMesh));
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

		bool HasSuperBorder
		{
			get { return delaunayMesh != null && delaunayMesh.HasSuperBorder; }
		}
	}
}
