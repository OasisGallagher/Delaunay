using System;
using System.Collections.Generic;
using UnityEngine;

namespace Delaunay
{
	public class Stage : MonoBehaviour
	{
		public float AgentRadius = 0.5f;
		public float AgentSpeed = 8f;
		
		GameObject destination;
		GameObject player;
		DelaunayMesh delaunayMesh;

		void Start()
		{
			destination = (GameObject)GameObject.Instantiate(Resources.Load("Prefabs/BallDest"));
			player = (GameObject)GameObject.Instantiate(Resources.Load("Prefabs/Player"));
			delaunayMesh = new DelaunayMesh();
			print(System.IO.Path.Combine(EditorConstants.kOutputFolder, "delaunay.dm"));
			delaunayMesh.Load(System.IO.Path.Combine(EditorConstants.kOutputFolder, "delaunay.dm"));
		}
		
		void Update()
		{
			Vector3 point = Vector3.zero;
			if (Input.GetMouseButtonUp(2) && GetScreenMousePosition(out point))
			{
				player.transform.position = delaunayMesh.GetNearestPoint(point, AgentRadius);
				player.GetComponent<Steering>().Path = null;
			}

			if (Input.GetMouseButtonUp(1) && GetScreenMousePosition(out point))
			{
				Vector3 src = player.transform.position;
				Vector3 dest = delaunayMesh.GetNearestPoint(point, AgentRadius);
				player.GetComponent<Steering>().Path = delaunayMesh.FindPath(src, dest, AgentRadius);
				destination.transform.position = dest;
			}

			Vector3 scale = new Vector3(AgentRadius * 2f, 1, AgentRadius * 2f);
			player.transform.localScale = scale;

			player.GetComponent<Steering>().Speed = AgentSpeed;
		}

		bool GetScreenMousePosition(out Vector3 point)
		{
			RaycastHit hit;
			point = Vector3.zero;
			if (!Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 100))
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
