using UnityEditor;
using UnityEngine;

namespace Delaunay
{
	public class SceneEditor : EditorWindow
	{
		public static SceneEditor Instance { get; private set; }

		DebugDraw debugDraw;

		[MenuItem("Window/Scene Editor")]
		static void OpenEditor()
		{
			if (Instance == null)
			{
				Instance = EditorWindow.GetWindow<SceneEditor>(false, "Scene Editor");
				Instance.Initialize();
			}
		}

		void Initialize()
		{
			SceneView.onSceneGUIDelegate += OnUpdateSceneGUI;
			debugDraw = new DebugDraw();
		}

		void OnUpdateSceneGUI(SceneView sceneView)
		{
			if (debugDraw != null)
			{
				debugDraw.Draw();
			}

			Vector3 hit = Vector3.zero;
			if (GetMouseClickPosition(out hit))
			{
				Debug.Log(hit);
			}
		}

		void OnDestroy()
		{
			SceneView.onSceneGUIDelegate -= OnUpdateSceneGUI;
			Instance = null;
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
		}

		bool GetMouseClickPosition(out Vector3 point)
		{
			point = Vector3.zero;
			if (!Input.GetMouseButtonUp(1)) { return false; }

			Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
			RaycastHit hit;
			if (!Physics.Raycast(ray, out hit, 100, -1))
			{
				Debug.LogError("Raycast failed");
				return false;
			}

			point = hit.point;
			point.y = 0;

			return true;
		}
	}
}
