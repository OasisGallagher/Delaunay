using System;
using System.Collections.Generic;
using UnityEngine;

namespace Delaunay
{
	public class Stage : MonoBehaviour
	{
		public bool ShowConvexHull = false;
		
		public float AgentRadius = 0.5f;
		public float AgentSpeed = 8f;

		DelaunayMesh delaunayMesh;
		
		GameObject destination;
		GameObject player;

		bool createObstacle = true;
		List<Vector3> newObstacle = new List<Vector3>();

		List<Vector3> borderCorners = new List<Vector3>();

		public void __tmpRemoveObstacle(int ID)
		{
			delaunayMesh.RemoveObstacle(ID);
		}

		#region Mono behaviour
		void Start()
		{
			GameObject floor = GameObject.Find("Floor");

			Vector3 scale = floor.transform.localScale / 2f;
			const float padding = 0.2f;

			Vector3 floorPosition = floor.transform.position;
			Rect rect = new Rect(floorPosition.x - scale.x - padding, floorPosition.z - scale.z - padding, (scale.x + padding) * 2, (scale.z + padding) * 2);
			delaunayMesh = new DelaunayMesh(rect);

			destination = (GameObject)GameObject.Instantiate(Resources.Load("Prefabs/BallDest"));
			player = (GameObject)GameObject.Instantiate(Resources.Load("Prefabs/Player"));

			borderCorners.Add(new Vector3(rect.xMax, 0, rect.yMax));
			borderCorners.Add(new Vector3(rect.xMin, 0, rect.yMax));
			borderCorners.Add(new Vector3(rect.xMin, 0, rect.yMin));
			borderCorners.Add(new Vector3(rect.xMax, 0, rect.yMin));

			DelaunayTest();
		}

		void DelaunayTest()
		{
			delaunayMesh.__tmpStart();

			//delaunayMesh.AddObstacle(triangle, true);
			/*
			Vector3[] localCircle = new Vector3[3];
			float deltaRadian = 2 * Mathf.PI / localCircle.Length;
			for (int i = 0; i < localCircle.Length; ++i)
			{
				localCircle[i].Set(Mathf.Cos(i * deltaRadian), 0, Mathf.Sin(i * deltaRadian));
			}

			Vector3[] circle = new Vector3[localCircle.Length];
			delaunayMesh.AddObstacle(localCircle.transform(circle, item => { return item * 1.5f + new Vector3(2, 0, 0); }), true);
			delaunayMesh.AddObstacle(localCircle.transform(circle, item => { return item * 1.5f + new Vector3(-2, 0, 0); }), true);
			delaunayMesh.AddObstacle(localCircle.transform(circle, item => { return item * 1.5f + new Vector3(-6, 0, 0); }), true);
			delaunayMesh.AddObstacle(localCircle.transform(circle, item => { return item * 1.5f + new Vector3(6, 0, 0); }), true);
			
			Vector3[] localSquare = new Vector3[4];
			localSquare[0] = new Vector3(0.5f, 0f, 0.5f);
			localSquare[1] = new Vector3(-0.5f, 0f, 0.5f);
			localSquare[2] = new Vector3(-0.5f, 0f, -0.5f);
			localSquare[3] = new Vector3(0.5f, 0f, -0.5f);
			
			Vector3[] square = new Vector3[localSquare.Length];
			
			delaunayMesh.AddObstacle(localSquare.transform(square, item => { return item * 2f + new Vector3(-2, 0, 5); }), true);
			delaunayMesh.AddObstacle(localSquare.transform(square, item => { return item * 2f + new Vector3(-2, 0, -5); }), true);
			delaunayMesh.AddObstacle(localSquare.transform(square, item => { return item * 2f + new Vector3(2, 0, 5); }), true);
			delaunayMesh.AddObstacle(localSquare.transform(square, item => { return item * 2f + new Vector3(2, 0, -5); }), true);
			delaunayMesh.AddObstacle(localSquare.transform(square, item => { return item * 2f + new Vector3(-6, 0, 5); }), true);
			delaunayMesh.AddObstacle(localSquare.transform(square, item => { return item * 2f + new Vector3(-6, 0, -5); }), true);
			delaunayMesh.AddObstacle(localSquare.transform(square, item => { return item * 2f + new Vector3(6, 0, 5); }), true);
			delaunayMesh.AddObstacle(localSquare.transform(square, item => { return item * 2f + new Vector3(6, 0, -5); }), true);
			*/
			delaunayMesh.AddBorder(borderCorners);
			
			delaunayMesh.__tmpStop();

			player.transform.position = delaunayMesh.GetNearestPoint(player.transform.position, AgentRadius);
			destination.transform.position = player.transform.position;
		}
		
		void Update()
		{
			Vector3 point = Vector3.zero;
			if (Input.GetMouseButtonUp(2) && GetScreenMousePosition(out point))
			{
				if (createObstacle)
				{
					if (AddNewObstacleVertex(point))
					{
						newObstacle.Add(point);
					}
					else
					{
						Debug.LogError("Invalid point " + point);
					}
				}
				else
				{
					player.transform.position = delaunayMesh.GetNearestPoint(point, AgentRadius);
					player.GetComponent<Steering>().Path = null;
				}
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

		void OnDrawGizmos()
		{
			foreach (Vector3 position in newObstacle)
			{
				Gizmos.DrawWireSphere(position + EditorConstants.kTriangleMeshOffset, 0.2f);
			}

			for (int i = 1; i < newObstacle.Count; ++i)
			{
				Gizmos.DrawLine(newObstacle[i - 1] + EditorConstants.kTriangleMeshOffset,
					newObstacle[i] + EditorConstants.kTriangleMeshOffset
				);
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
					Vertex.vertexID = 10000;
					Triangle.triangleID = 10000;
					HalfEdge.halfEdgeID = 10000;
					Obstacle.obstacleID = 10000;
				}
			}

			bool toggle = GUILayout.Toggle(createObstacle, "Plant");
			if (toggle != createObstacle)
			{
				createObstacle = toggle;
				if (newObstacle.Count > 0)
				{
					print(string.Join("\t", newObstacle.toStrArray()));
					delaunayMesh.AddObstacle(newObstacle);
					newObstacle.Clear();
				}
			}

			GUILayout.Label("V: " + GeomManager.AllVertices.Count);
			GUILayout.Label("E: " + GeomManager.AllEdges.Count);
			GUILayout.Label("T: " + GeomManager.AllTriangles.Count);

			GUILayout.EndVertical();
		}

		#endregion

		void Clear()
		{
			if (delaunayMesh != null) { delaunayMesh.Clear(); }
			destination.SetActive(false);
		}

		bool AddNewObstacleVertex(Vector3 point)
		{
			if (newObstacle.Count == 0) { return true; }
			Vector3 cross = Vector3.zero;
			for (int i = 1; i < newObstacle.Count; ++i)
			{
				CrossState state = MathUtility.GetLineCrossPoint(out cross, newObstacle[i],
					newObstacle[i - 1], point, newObstacle.back());

				if (state == CrossState.CrossOnSegment && !cross.equals2(newObstacle.back()))
				{
					return false;
				}
			}

			return true;
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
