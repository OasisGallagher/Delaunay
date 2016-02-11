﻿using System;
using UnityEngine;

namespace Delaunay
{
	public class Stage : MonoBehaviour
	{
		public bool ShowConvexHull = false;
		public bool ShowFindBoundTrianglePath = false;

		DelaunayMesh delaunayMesh;
		RaycastHit hit;

		#region Mono behaviour
		void Start()
		{
			GameObject floor = GameObject.Find("Floor");

			Vector3 scale = floor.transform.localScale / 2f;
			const float padding = 0.2f;

			Vector3 floorPosition = floor.transform.position;
			Rect rect = new Rect(floorPosition.x - scale.x - padding, floorPosition.z - scale.z - padding, (scale.x + padding) * 2, (scale.z + padding) * 2);
			delaunayMesh = new DelaunayMesh(rect);
		}

		void Update()
		{
			//if (Input.GetMouseButtonUp(0) && EditorParameter.plant
			//	&& Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 100))
			//{
			//}
		}

		void OnDrawGizmos()
		{
			if (delaunayMesh != null) { delaunayMesh.OnDrawGizmos(ShowFindBoundTrianglePath, ShowConvexHull); }
		}

		void OnGUI()
		{
			GUILayout.BeginVertical("Box", GUILayout.Width(EditorConstants.kPanelWidth));

			if (GUILayout.Button("Save"))
			{
				string path = UnityEditor.EditorUtility.SaveFilePanel("", Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "delaunay", "xml");
				SerializeTools.Save(path);
			}

			if (GUILayout.Button("Load"))
			{
				GeomManager.Clear();
				string path = UnityEditor.EditorUtility.OpenFilePanel("", Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "xml");
				SerializeTools.Load(path);
			}

			GUILayout.EndVertical();
		}

		#endregion
	}
}
