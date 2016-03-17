using UnityEditor;
using UnityEngine;

namespace Delaunay
{
	public class SceneEditor : EditorWindow
	{
		public static SceneEditor Instance { get; private set; }

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
		}

		void OnUpdateSceneGUI(SceneView sceneView)
		{
		}

		void OnDestroy()
		{
			SceneView.onSceneGUIDelegate -= OnUpdateSceneGUI;
			Instance = null;
		}
	}
}
