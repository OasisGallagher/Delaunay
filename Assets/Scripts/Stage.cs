using System;
using System.Collections.Generic;
using UnityEngine;

namespace Delaunay
{
	public class Stage : MonoBehaviour
	{
		public bool ShowConvexHull = false;
		public bool __tmpShowTangents = false;

		public float AgentRadius = 0.5f;

		DelaunayMesh delaunayMesh;
		
		GameObject destination;
		GameObject player;

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

			Vector3[] localCircle = new Vector3[7];
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
			delaunayMesh.AddObstacle(borderCorners, false);

			delaunayMesh.__tmpStop();

			player.transform.position = delaunayMesh.GetNearestPoint(player.transform.position);
			destination.transform.position = player.transform.position;
		}

		void Update()
		{
			Vector3 point = Vector3.zero;
			if (Input.GetMouseButtonUp(2) && GetScreenMousePosition(out point))
			{
				player.transform.position = delaunayMesh.GetNearestPoint(point);
				player.GetComponent<Steering>().Path = null;
			}

			if (Input.GetMouseButtonUp(1) && GetScreenMousePosition(out point))
			{
				Vector3 src = player.transform.position;
				Vector3 dest = delaunayMesh.GetNearestPoint(point);
				player.GetComponent<Steering>().Path = delaunayMesh.FindPath(src, dest, AgentRadius);
				destination.transform.position = dest;
			}

			Vector3 scale = new Vector3(AgentRadius, 1, AgentRadius);
			player.transform.localScale = scale;
		}

		void OnDrawGizmos()
		{
			if (delaunayMesh != null)
			{
				delaunayMesh.OnDrawGizmos(ShowConvexHull);
			}

			if (__tmpShowTangents)
			{
				int index = 0;
				Color[] array = new Color[] { Color.red, Color.green, Color.blue };
				foreach (Tuple2<Vector3, Vector3> tangent in NonPointObjectFunnel.tmpTangents)
				{
					Color oldGizmosColor = Gizmos.color;
					Gizmos.color = array[index % array.Length];
					tangent.First.y = tangent.Second.y = EditorConstants.kConvexHullGizmosHeight;
					Gizmos.DrawLine(tangent.First, tangent.Second);
					Gizmos.color = oldGizmosColor;
					++index;
				}
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
					if (!obstacle.__tmpActive)
					{
						(target as Stage).__tmpRemoveObstacle(obstacle.ID);
					}
					//obstacle.Mesh.ForEach(item => { item.gameObject.SetActive(active); });
				}
			}
		}
	}
}
