using System;
using System.Collections.Generic;
using UnityEngine;

namespace Delaunay
{
	public class Stage : MonoBehaviour
	{
		public bool ShowConvexHull = false;

		DelaunayMesh delaunayMesh;
		List<Vector3> currentPath;
		GameObject ballStart, ballDest;

		enum ClickState
		{
			PlantingStartPoint,
			PlantingEndPoint,
			Ready4Pathfinding,
		}

		ClickState clickState = ClickState.PlantingStartPoint;

		#region Mono behaviour
		void Start()
		{
			GameObject floor = GameObject.Find("Floor");

			Vector3 scale = floor.transform.localScale / 2f;
			const float padding = 0.2f;

			Vector3 floorPosition = floor.transform.position;
			Rect rect = new Rect(floorPosition.x - scale.x - padding, floorPosition.z - scale.z - padding, (scale.x + padding) * 2, (scale.z + padding) * 2);
			delaunayMesh = new DelaunayMesh(rect);

			ballStart = (GameObject)GameObject.Instantiate(Resources.Load("Prefabs/BallStart"));
			ballDest = (GameObject)GameObject.Instantiate(Resources.Load("Prefabs/BallDest"));

			ballStart.SetActive(false);
			ballDest.SetActive(false);
		}

		void Update()
		{
			RaycastHit hit;

			if (Input.GetMouseButtonUp(0)
				&& Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 100))
			{
				if (clickState == ClickState.PlantingStartPoint)
				{
					ballStart.SetActive(true);
					ballDest.SetActive(false);

					currentPath = null;

					ballStart.transform.position = hit.point;
					clickState = ClickState.PlantingEndPoint;
				}
				else if (clickState == ClickState.PlantingEndPoint)
				{
					ballDest.SetActive(true);
					ballDest.transform.position = hit.point;
					clickState = ClickState.Ready4Pathfinding;
				}
			}

			if (clickState == ClickState.Ready4Pathfinding)
			{
				Vector3 start = ballStart.transform.position;
				Vector3 dest = ballDest.transform.position;
				currentPath = delaunayMesh.FindPath(start, dest);
				if (currentPath == null)
				{
					print("no path from " + start + " to " + dest);
				}

				clickState = ClickState.PlantingStartPoint;
			}
		}

		void OnDrawGizmos()
		{
			if (delaunayMesh != null) { delaunayMesh.OnDrawGizmos(ShowConvexHull); }
			if (currentPath != null)
			{
				DrawCurrentPath();
			}
		}

		void OnGUI()
		{
			GUILayout.BeginVertical("Box", GUILayout.Width(EditorConstants.kPanelWidth));

			if (GUILayout.Button("Save"))
			{
				string path = UnityEditor.EditorUtility.SaveFilePanel("", Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "delaunay", "xml");
				if (!string.IsNullOrEmpty(path))
				{
					SerializeTools.Save(path);
				}
			}

			if (GUILayout.Button("Load"))
			{
				string path = UnityEditor.EditorUtility.OpenFilePanel("", Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "xml");
				if (!string.IsNullOrEmpty(path))
				{
					Clear();
					SerializeTools.Load(path);
					print(path + " loaded.");
				}
			}

			GUILayout.EndVertical();
		}

		void DrawCurrentPath()
		{
			for (int i = 1; i < currentPath.Count; ++i)
			{
				Debug.DrawLine(currentPath[i - 1] + EditorConstants.kPathRendererOffset,
					currentPath[i] + EditorConstants.kPathRendererOffset, Color.yellow
				);
			}
		}

		#endregion

		void Clear()
		{
			if (delaunayMesh != null) { delaunayMesh.Clear(); }
			ballStart.SetActive(false);
			ballDest.SetActive(false);
			currentPath.Clear();
		}
	}

	[UnityEditor.CustomEditor(typeof(Stage))]
	public class StageEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			foreach (Obstacle obstacle in GeomManager.AllObstacles)
			{
				bool active = GUILayout.Toggle(obstacle.__tmpActive, "Obstacle " + obstacle.ID);
				if (active != obstacle.__tmpActive)
				{
					obstacle.__tmpActive = active;
					obstacle.Mesh.ForEach(item => { item.gameObject.SetActive(active); });
				}
			}
		}
	}
}
