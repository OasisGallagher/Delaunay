using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Delaunay
{
	public class Stage : MonoBehaviour
	{
		public DelaunayMesh delaunayMesh;

		/// <summary>
		/// 场景的宽度.
		/// </summary>
		public float Width { get { return delaunayMesh.Width; } }

		/// <summary>
		/// 场景的高度.
		/// </summary>
		public float Height { get { return delaunayMesh.Height; } }

		/// <summary>
		/// 场景的原点.
		/// </summary>
		public Vector3 Origin { get { return delaunayMesh.Origin; } }

		/// <summary>
		/// 由point向下发射射线, 返回与场景的collider的碰撞点.
		/// </summary>
		public Vector3 PhysicsHeightTest(Vector3 point)
		{
			point.y = 25f;
			RaycastHit hit;
			if (!Physics.Raycast(point, Vector3.down, out hit, 100f))
			{
				Debug.LogError("Raycast failed");
				return point;
			}

			return hit.point;
		}

		void Awake()
		{
			delaunayMesh = new DelaunayMesh(new Vector3(-10, 0, -10), 20f, 20f);

			string path = Path.Combine(EditorConstants.kOutputFolder, "delaunay.dm").Replace('\\', '/');
			delaunayMesh.Load(path);
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
	}
}
