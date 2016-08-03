using System.Collections.Generic;
using UnityEngine;

namespace Delaunay
{
	public class StressTest : MonoBehaviour
	{
		public int playerCount = 1;

		class TestCase
		{
			public float repathRemaining;
			public PlayerComponent player;
		}

		Stage stage;
		List<TestCase> testCases;

		void Awake()
		{
			stage = GetComponent<Stage>();
			testCases = new List<TestCase>(); 

			UpdatePlayerCount(playerCount);
		}

		void OnDestroy()
		{
			DestroyTestCases();
		}

		void Update()
		{
			foreach (TestCase test in testCases)
			{
				test.repathRemaining -= Time.deltaTime;

				if (test.repathRemaining <= 0)
				{
					Vector3 dest = GetRandomPosition(test.player.Radius);
					Vector3 src = test.player.transform.position;

					List<Vector3> path = stage.delaunayMesh.FindPath(src, dest, test.player.Radius);
					test.player.GetComponent<Steering>().SetPath(path);

					test.repathRemaining = Random.Range(1f, 5f);
				}
			}
		}

		void DestroyTestCases()
		{
			for (int i = 0; i < testCases.Count; ++i)
			{
				//testCases[i].player.GetComponent<Steering>().onPositionChanged -= OnPlayerMove;
				//stage.delaunayMesh.RemoveObstacle(testCases[i].territory.ID);
				//GameObject.Destroy(testCases[i].player.gameObject);
			}

			testCases.Clear();
		}

		void UpdatePlayerCount(int count)
		{
			playerCount = count;

			for (int i = 0; i < count; ++i)
			{
				GameObject player = (GameObject)GameObject.Instantiate(Resources.Load("Prefabs/Player"));
				player.name = "player " + (i + 1);

				Steering steering = player.GetComponent<Steering>();
				steering.SetTerrain(stage.delaunayMesh);
				steering.onPositionChanged += OnPlayerMove;

				PlayerComponent pc = player.GetComponent<PlayerComponent>();
				Vector3 position = GetRandomPosition(pc.Radius);
				pc.transform.position = position;

				testCases.Add(new TestCase { player = pc, repathRemaining = 0 });
			}
		}

		void OnPlayerMove(PlayerComponent player, Vector3 oldPosition, Vector3 newPosition)
		{
		}

		Vector3 GetRandomPosition(float radius)
		{
			float w = Random.value * stage.Width;
			float h = Random.value * stage.Height;

			Vector3 pos = new Vector3(w, 0, h) + stage.Origin;
			pos = stage.PhysicsHeightTest(pos);
			pos = stage.delaunayMesh.GetNearestPoint(pos, radius);
			return pos;
		}

		Vector3[] CalculateCircleVertices(Vector3 center, float radius)
		{
			int vertexCount = 360 / 120;

			Vector3[] ans = new Vector3[vertexCount];

			Vector3 point = center + Vector3.forward * radius;
			float radianStep = -Mathf.PI * 2f / vertexCount;

			for (int i = 0; i < vertexCount; ++i)
			{
				ans[i] = MathUtility.Rotate(point, i * radianStep, center);
			}

			return ans;
		}
	}
}
