using System.Collections.Generic;
using UnityEngine;

namespace Delaunay
{
	public class StressTest : MonoBehaviour
	{
		public int PlayerCount
		{
			get { return playerCount; }
			set { if (playerCount != value) { UpdatePlayerCount(playerCount); } }
		}

		class TestCase
		{
			public int territory;
			public float repathRemaining;
			public PlayerComponent player;
		}

		Stage stage;
		int playerCount = 10;

		List<TestCase> testCases = new List<TestCase>();

		void Awake()
		{
			stage = GetComponent<Stage>();
			GetComponent<Steering>().onPositionChanged += OnPlayerMove;
			UpdatePlayerCount(playerCount);
		}

		void OnDestroy()
		{
			GetComponent<Steering>().onPositionChanged -= OnPlayerMove;
		}

		void Update()
		{
			for (int i = 0; i < testCases.Count; ++i)
			{
				TestCase test = testCases[i];
				test.repathRemaining -= Time.deltaTime;

				if (test.repathRemaining <= 0)
				{
					Vector3 dest = GetRandomPosition(test.player.Radius);
					Vector3 src = test.player.transform.position;
					test.player.GetComponent<Steering>().SetPath(stage.delaunayMesh.FindPath(src, dest, test.player.Radius));
					test.repathRemaining = Random.Range(1f, 5f);
				}
			}
		}

		void UpdatePlayerCount(int count)
		{
			for (int i = 0; i < testCases.Count; ++i)
			{
				stage.delaunayMesh.RemoveObstacle(testCases[i].territory);
				GameObject.Destroy(testCases[i].player.gameObject);
			}

			testCases.Clear();

			playerCount = count;

			for (int i = 0; i < count; ++i)
			{
				GameObject player = (GameObject)GameObject.Instantiate(Resources.Load("Prefabs/Player"));
				player.name = "player " + (i + 1);

				player.GetComponent<Steering>().SetTerrain(stage.delaunayMesh);

				PlayerComponent pc = player.GetComponent<PlayerComponent>();
				pc.transform.position = GetRandomPosition(pc.Radius);

				Vector3[] circleVertices = CalculateCircleVertices(pc.transform.position, pc.Radius);
				int territory = stage.delaunayMesh.AddObstacle(circleVertices).ID;
				testCases.Add(new TestCase { player = pc, territory = territory });
			}
		}

		void OnPlayerMove(PlayerComponent player, Vector3 oldPosition, Vector3 newPosition)
		{
			int index = testCases.FindIndex(item => { return item.player == player; });
			stage.delaunayMesh.RemoveObstacle(testCases[index].territory);
			testCases[index].territory = stage.delaunayMesh.AddObstacle(CalculateCircleVertices(newPosition, player.Radius)).ID;
		}

		Vector3 GetRandomPosition(float radius)
		{
			float w = MathUtility.GetUniqueRandomInteger() % stage.Width;
			float h = MathUtility.GetUniqueRandomInteger() % stage.Height;

			Vector3 pos = new Vector3(w, 0, h) + stage.Origin;
			pos = stage.PhysicsHeightTest(pos);
			pos = stage.delaunayMesh.GetNearestPoint(pos, radius);
			return pos;
		}

		Vector3[] CalculateCircleVertices(Vector3 center, float radius)
		{
			int vertexCount = 360 / 30;

			Vector3[] ans = new Vector3[vertexCount];

			Vector3 point = center + Vector3.forward * radius;
			float radianStep = Mathf.PI * 2f / vertexCount;

			for (int i = 0; i < vertexCount; ++i)
			{
				ans[i] = MathUtility.Rotate(point, i * radianStep, center);
			}

			return ans;
		}
	}
}
