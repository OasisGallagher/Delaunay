﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
namespace Delaunay
{
	public interface IPathNode
	{
		HalfEdge Portal { get; set; }
		HalfEdge[] AdjPortals { get; }

		float G { get; set; }
		float H { get; set; }
	}

	public static class Pathfinding
	{
		public static List<Vector3> FindPath(Vector3 startPosition, Vector3 destPosition, IPathNode startNode, IPathNode destNode)
		{
			List<HalfEdge> portals = AStarPathfinding.FindPath(startPosition, destPosition, startNode, destNode);
			return PathSmoother.Smooth(startPosition, destPosition, portals);
		}
	}

	internal static class AStarPathfinding
	{
		public static List<HalfEdge> FindPath(Vector3 startPosition, Vector3 destPosition, IPathNode startNode, IPathNode destNode)
		{
			BinaryHeap open = new BinaryHeap();
			CloseList close = new CloseList();
			
			startNode.G = 0;

			open.Push(startNode);

			for (; open.Count != 0 && startNode != destNode; )
			{
				startNode = open.Pop();

				foreach (HalfEdge current in startNode.AdjPortals)
				{
					if (!current.Face.Walkable || close.Contains(current.Face))
					{
						continue;
					}

					int index = open.IndexOf(current.Face);
					if (index < 0)
					{
						current.Face.Portal = null;
						current.Face.G = float.PositiveInfinity;
						current.Face.H = float.PositiveInfinity;

						open.Push(current.Face);
						index = open.Count - 1;
					}

					Utility.Verify(startNode.G == 0 || startNode.Portal != null);

					float tmp = startNode.G;

					if (startNode.Portal != null)
					{
						tmp += (startNode.Portal.Center - current.Center).magnitude2();
					}

					if (tmp < current.Face.G)
					{
						float h = Utility.MinDistance(destPosition, current.Src.Position, current.Dest.Position);
						Utility.Verify(current.Face.H + current.Face.G >= h + tmp, "Invalid F");

						current.Face.H = h;
						current.Face.G = tmp;
						open.AdjustHeap(index);
						current.Face.Portal = current;
					}
				}

				close.Add(startNode);
			}

			List<HalfEdge> path = null;
			if (startNode == destNode) { path = CreatePath(destNode); }

			open.Dispose();
			close.Dispose();

			return path;
		}

		static List<HalfEdge> CreatePath(IPathNode dest)
		{
			List<HalfEdge> result = new List<HalfEdge>();
			for(HalfEdge entry; (entry = dest.Portal) != null; dest = entry.Pair.Face)
			{
				Utility.Verify(result.Count < 1024, "Too many waypoints");
				result.Add(entry);
			}

			result.Reverse();

			return result;
		}

		internal class CloseList : IDisposable
		{
			public void Dispose()
			{
				foreach (IPathNode node in container) { node.Portal = null; }
			}

			public void Add(IPathNode item) 
			{
				container.Add(item);
			}

			public bool Contains(IPathNode item)
			{
				return container.Contains(item);
			}

			HashSet<IPathNode> container = new HashSet<IPathNode>();
		}

		internal class BinaryHeap : IDisposable
		{
			public void Dispose()
			{
				container.ForEach(item => { item.Portal = null; });
			}

			public void Push(IPathNode node)
			{
				container.Add(node);

				AdjustHeap(container.Count - 1);
			}

			public int Count { get { return container.Count; } }

			public IPathNode Pop()
			{
				Swap(0, container.Count - 1);
				IPathNode result = container[container.Count - 1];
				container.RemoveAt(container.Count - 1);

				int current = 0;
				for (; ; )
				{
					int min = current;
					int lchild = LeftChild(min), rchild = RightChild(min);
					if (lchild < container.Count && F(container[lchild]) < F(container[min]))
					{
						min = lchild;
					}

					if (rchild < container.Count && F(container[rchild]) < F(container[min]))
					{
						min = rchild;
					}

					if (min == current)
					{
						break;
					}

					Swap(min, current);

					current = min;
				}

				return result;
			}

			public void AdjustHeap(int from)
			{
				int parent = Parent(from);
				for (; parent >= 0 && F(container[from]) < F(container[parent]); )
				{
					Swap(from, parent);
					from = parent;
					parent = Parent(parent);
				}
			}

			// TODO: O(n).
			public int IndexOf(IPathNode node)
			{
				return container.IndexOf(node);
			}

			float F(IPathNode node) { return node.G + node.H; }

			bool IsHeap()
			{
				for (int i = 1; i < container.Count; ++i)
				{
					if (F(container[Parent(i)]) > F(container[i]))
					{
						return false;
					}
				}

				return true;
			}

			void Swap(int i, int j)
			{
				IPathNode tmp = container[i];
				container[i] = container[j];
				container[j] = tmp;
			}

			int Parent(int i) { return (i - 1) / 2; }

			int LeftChild(int i) { return 2 * i + 1; }

			int RightChild(int i) { return 2 * i + 2; }

			List<IPathNode> container = new List<IPathNode>();
		}
	}

	internal static class PathSmoother
	{
		public static List<Vector3> Smooth(Vector3 start, Vector3 dest, List<HalfEdge> edges)
		{
			Vector3[] portals = new Vector3[edges.Count * 2 + 4];
			portals[0] = portals[1] = start;
			int index = 2;

			foreach(HalfEdge edge in edges)
			{
				portals[index++] = edge.Src.Position;
				portals[index++] = edge.Dest.Position;
			}

			portals[index] = portals[index + 1] = dest;

			return StringPull(portals);
		}

		static List<Vector3> StringPull(Vector3[] portals)
		{
			Vector3 portalApex = portals[0];
			Vector3 portalLeft = portals[2];
			Vector3 portalRight = portals[3];

			List<Vector3> answer = new List<Vector3>(1 + portals.Length / 2);
			answer.Add(portalApex);

			int apexIndex = 0, leftIndex = 0, rightIndex = 0;
			for (int i = 1; i < portals.Length / 2; ++i)
			{
				Vector3 left = portals[i * 2];
				Vector3 right = portals[i * 2 + 1];

				if (right.cross2(portalRight, portalApex) <= 0)
				{
					if (portalApex.equals2(portalRight) || right.cross2(portalLeft, portalApex) > 0f)
					{
						portalRight = right;
						rightIndex = i;
					}
					else
					{
						answer.Add(portalLeft);
						portalApex = portalLeft;
						apexIndex = leftIndex;

						portalLeft = portalRight = portalApex;
						leftIndex = rightIndex = apexIndex;

						i = apexIndex;
						continue;
					}
				}

				if (left.cross2(portalLeft, portalApex) >= 0)
				{
					if (portalApex.equals2(portalLeft) || left.cross2(portalRight, portalApex) < 0f)
					{
						portalLeft = left;
						leftIndex = i;
					}
					else
					{
						answer.Add(portalRight);
						portalApex = portalRight;
						apexIndex = rightIndex;

						portalLeft = portalRight = portalApex;
						leftIndex = rightIndex = apexIndex;

						i = apexIndex;
						continue;
					}
				}
			}

			answer.Add(portals[portals.Length  -1]);

			return answer;
		}
	}
}