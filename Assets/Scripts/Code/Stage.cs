using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Delaunay
{
	public class Stage : MonoBehaviour
	{
		GameObject destination;
		GameObject player;
		PlayerComponent playerComponent;

		public DelaunayMesh delaunayMesh;

		void Start()
		{
			delaunayMesh = new DelaunayMesh();

			destination = (GameObject)GameObject.Instantiate(Resources.Load("Prefabs/BallDest"));
			player = (GameObject)GameObject.Instantiate(Resources.Load("Prefabs/Player"));
			player.GetComponent<Steering>().SetTerrain(delaunayMesh);

			playerComponent = player.GetComponent<PlayerComponent>();

			print(Path.Combine(EditorConstants.kOutputFolder, "delaunay.dm"));
			delaunayMesh.Load(Path.Combine(EditorConstants.kOutputFolder, "delaunay.dm"));
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

		void Update()
		{
			Vector3 point = Vector3.zero;
			if (Input.GetMouseButtonUp(2) && MousePositionToStage(out point))
			{
				player.transform.position = delaunayMesh.GetNearestPoint(point, playerComponent.Radius);
				player.GetComponent<Steering>().SetPath(null);
			}

			if (Input.GetMouseButtonUp(1) && MousePositionToStage(out point))
			{
				Vector3 src = player.transform.position;
				Vector3 dest = delaunayMesh.GetNearestPoint(point, playerComponent.Radius);
				player.GetComponent<Steering>().SetPath(delaunayMesh.FindPath(src, dest, playerComponent.Radius));
				destination.transform.position = dest;
			}

			Vector3 scale = new Vector3(playerComponent.Radius * 2f, 1, playerComponent.Radius * 2f);
			player.transform.localScale = scale;
		}

		bool MousePositionToStage(out Vector3 point, Vector3? src = null)
		{
			RaycastHit hit;
			point = Vector3.zero;
			if (!Physics.Raycast(Camera.main.ScreenPointToRay(src ?? Input.mousePosition), out hit, 100))
			{
				Debug.LogError("Raycast failed");
				return false;
			}

			point = hit.point;

			return true;
		}
	}
}
