using System;
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
			return PathSmoother.Smooth(startPosition, destPosition, portals, 0.5f);
		}
	}

	internal static class AStarPathfinding
	{
		public static List<HalfEdge> FindPath(Vector3 startPosition, Vector3 destPosition, IPathNode startNode, IPathNode destNode)
		{
			float radius = 0f;
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

					float newH = MathUtility.MinDistance(destPosition, current.Src.Position, current.Dest.Position);

					float newG = startNode.G;
					if (startNode.Portal != null)
					{
						newG += (startNode.Portal.Center - current.Center).magnitude2();
					}

					if (current.Face.GetWidth(current.Src) < radius || current.Face.GetWidth(current.Dest) < radius)
					{ 
						
					}

					if (current.Pair.Face != null)
					{
						current.Pair.Face.Portal;
						current.Pair;
					}

					if (newG + newH < current.Face.G + current.Face.H)
					{
						current.Face.H = newH;
						current.Face.G = newG;
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
			for (HalfEdge entry; (entry = dest.Portal) != null; dest = entry.Pair.Face)
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
		public static List<Vector3> Smooth(Vector3 start, Vector3 dest, List<HalfEdge> edges, float radius)
		{
			List<Vector3> portals = new List<Vector3>(edges.Count * 2 + 4) { start, start };
			portals[0] = portals[1] = start;

			foreach (HalfEdge edge in edges)
			{
				portals.Add(edge.Src.Position);
				portals.Add(edge.Dest.Position);
			}

			portals.Add(dest);
			portals.Add(dest);

			NonPointObjectFunnel.tmpTangents.Clear();

			return NonPointObjectFunnel.Funnel(portals, radius);
		}
	}

	/// <summary>
	/// http://www.koffeebird.com/2014/05/towards-modified-simple-stupid-funnel.html
	/// </summary>
	internal static class NonPointObjectFunnel
	{
		enum ApexType
		{
			Left,
			Right,
			Point,
		}

		struct Apex
		{
			public Vector3 position;
			public ApexType type;
		}

		// This algorithm is basically the Simple Stupid Funnel Algorithm posted by
		// Mikko in the Digesting Duck blog. This one has been modified to account for agent radius.
		public static List<Vector3> Funnel(List<Vector3> portals, float radius)
		{
			// In some special cases, it is possible that the tangents of the apexes will
			// cause the funnel to collapse to the left or right portal right before going to
			// the final position. This happens when the final position is more 'outward' than
			// the vector from the apex to the portal extremity, and the final position is
			// actually 'closer' to the previous portal than the 'current' portal extremity.
			// If that happens, we remove the portal before the last from the list. I have no
			// proof that this guarantees the correct behavior, though.
			if (portals.Count >= 8)
			{
				// This seems to be possible to happen only when there are 4 or more
				// portals (first and last are start and destination)
				int basePortal = portals.Count - 6;
				int lastPortal = portals.Count - 4;
				int destinationPortal = portals.Count - 2;

				// First, check left
				Vector3 baseLast = portals[lastPortal] - portals[basePortal];
				Vector3 baseDest = portals[destinationPortal] - portals[basePortal];
				if (baseDest.sqrMagnitude2() < baseLast.sqrMagnitude2())
				{
					portals.RemoveRange(lastPortal, 2);
				}
				else
				{
					// Now check right
					baseLast = portals[lastPortal + 1] - portals[basePortal + 1];
					baseDest = portals[destinationPortal + 1] - portals[basePortal + 1];
					if (baseDest.sqrMagnitude2() < baseLast.sqrMagnitude2())
					{
						portals.RemoveRange(lastPortal, 2);
					}
				}
			}

			Vector3 portalApex = portals[0];
			Vector3 portalLeft = portals[0];
			Vector3 portalRight = portals[1];

			int portalLeftIndex = 0;
			int portalRightIndex = 0;

			// Put the first point into the contact list
			Apex startApex = new Apex();
			startApex.position = portalApex;
			startApex.type = ApexType.Point;

			List<Apex> contactVertices = new List<Apex>();

			contactVertices.Add(startApex);

			ApexType currentType = ApexType.Point;
			Vector3 previousValidLSegment = Vector3.zero;
			Vector3 previousValidRSegment = Vector3.zero;

			for (int i = 2; i < portals.Count; i += 2)
			{
				Vector3 left = portals[i];
				Vector3 right = portals[i + 1];

				ApexType nextLeft = ApexType.Left;
				ApexType nextRight = ApexType.Right;

				if (i >= portals.Count - 2)
				{
					// Correct next apex type if we are at the end of the channel
					nextLeft = ApexType.Point;
					nextRight = ApexType.Point;
				}

				// Build radius-inflated line segments
				Tuple2<Vector3, Vector3> tuple = new Tuple2<Vector3, Vector3>(portalApex, left);
				//Vector3 tempA = portalApex, tempB = left;
				//GetTangentPoints(out tempA, out tempB, tempA, tempB, currentType, nextLeft, radius);
				tuple = GetTangentPoints(currentType, tuple.First, nextLeft, tuple.Second, radius);
				Vector3 currentLSegment = tuple.Second - tuple.First;

				//tempA = portalApex; tempB = right;
				tuple.Set(portalApex, right);
				//GetTangentPoints(out tempA, out tempB, tempA, tempB, currentType, nextRight, radius);
				tuple = GetTangentPoints(currentType, tuple.First, nextRight, tuple.Second, radius);
				Vector3 currentRSegment = tuple.Second - tuple.First;

				//Right side
				// Does new 'right' reduce the funnel?
				//if (MyMath2D.CrossProduct2D(previousValidRSegment, currentRSegment) > -MyMath2D.tolerance)
				if (previousValidRSegment.cross2(currentRSegment) >= 0)
				{
					// Does it NOT cross the left side?
					// Is the apex the same as portal right? (if true, no chance but to move)
					if (
						portalApex.equals2(portalRight) ||
						previousValidLSegment.cross2(currentRSegment) <= 0
						//MyMath2D.CrossProduct2D(previousValidLSegment, currentRSegment) < MyMath2D.tolerance
					)
					{
						portalRight = right;
						previousValidRSegment = currentRSegment;
						portalRightIndex = i;
					}
					else
					{
						// Collapse
						if (currentRSegment.sqrMagnitude2() > previousValidLSegment.sqrMagnitude2())
						{
							portalApex = portalLeft;
							portalRight = portalApex;

							Apex apex = new Apex();
							apex.position = portalApex;
							apex.type = ApexType.Left;
							contactVertices.Add(apex);

							currentType = ApexType.Left;

							portalRightIndex = portalLeftIndex;
							i = portalLeftIndex;
						}
						else
						{
							portalRight = right;
							previousValidRSegment = currentRSegment;
							portalRightIndex = i;

							portalApex = portalRight;
							portalLeft = portalApex;

							Apex apex = new Apex();
							apex.position = portalApex;
							apex.type = ApexType.Right;
							contactVertices.Add(apex);

							currentType = ApexType.Right;

							portalLeftIndex = portalRightIndex;
							i = portalRightIndex;
						}

						previousValidLSegment = Vector3.zero;
						previousValidRSegment = Vector3.zero;

						continue;
					}
				}

				// Left Side
				// Does new 'left' reduce the funnel?
				//if (MyMath2D.CrossProduct2D(previousValidLSegment, currentLSegment) < MyMath2D.tolerance)
				if (previousValidLSegment.cross2(currentLSegment) <= 0)
				{
					// Does it NOT cross the right side?
					// Is the apex the same as portal left? (if true, no chance but to move)
					if (portalApex.equals2(portalLeft) ||
						previousValidRSegment.cross2(currentLSegment) >= 0
						//MyMath2D.CrossProduct2D(previousValidRSegment, currentLSegment) > -MyMath2D.tolerance
					)
					{
						portalLeft = left;
						previousValidLSegment = currentLSegment;
						portalLeftIndex = i;
					}
					else
					{
						// Collapse
						if (currentLSegment.sqrMagnitude2() > previousValidRSegment.sqrMagnitude2())
						{
							portalApex = portalRight;
							portalLeft = portalApex;

							Apex apex = new Apex();
							apex.position = portalApex;
							apex.type = ApexType.Right;
							contactVertices.Add(apex);

							currentType = ApexType.Right;

							portalLeftIndex = portalRightIndex;
							i = portalRightIndex;
						}
						else
						{
							portalLeft = left;
							previousValidLSegment = currentLSegment;
							portalLeftIndex = i;

							portalApex = portalLeft;
							portalRight = portalApex;

							Apex apex = new Apex();
							apex.position = portalApex;
							apex.type = ApexType.Left;
							contactVertices.Add(apex);

							currentType = ApexType.Left;

							portalRightIndex = portalLeftIndex;
							i = portalLeftIndex;
						}

						previousValidLSegment = Vector3.zero;
						previousValidRSegment = Vector3.zero;

						continue;
					}
				}
			}

			// Put the last point into the contact list
			if (contactVertices[contactVertices.Count - 1].position.equals2(portals[portals.Count - 1]))
			{
				// Last point was added to funnel, so we need to change its type to point
				Apex endApex = new Apex();
				endApex.position = portals[portals.Count - 1];
				endApex.type = ApexType.Point;
				contactVertices[contactVertices.Count - 1] = endApex;
			}
			else
			{
				// Last point was not added to funnel, so we add it
				Apex endApex = new Apex();
				endApex.position = portals[portals.Count - 1];
				endApex.type = ApexType.Point;
				contactVertices.Add(endApex);
			}

			return BuildPath(radius, contactVertices);
		}

		static List<Vector3> BuildPath(float radius, List<Apex> contactVertices)
		{
			List<Vector3> path = new List<Vector3>();

			// My channel actually goes from path end to path start, so I need to
			// invert all the apexes sides...

			// Add first node
			//path.Add(contactVertices[contactVertices.Count - 1].position);
			path.Add(contactVertices[0].position);

			/*for (int i = contactVertices.Count - 2; i >= 0; --i)*/
			for (int i = 1; i < contactVertices.Count; ++i)
			{
				Tuple2<Vector3, Vector3> tuple = GetTangentPoints(contactVertices[i - 1], contactVertices[i], radius);

				path.Add(tuple.First);
				path.Add(tuple.Second);
			}

			return path;
		}

		static Tuple2<Vector3, Vector3> GetTangentPoints(Apex apex1, Apex apex2, float radius)
		{
			return GetTangentPoints(apex1.type, apex1.position, apex2.type, apex2.position, radius);
		}

		public static List<Tuple2<Vector3, Vector3>> tmpTangents = new List<Tuple2<Vector3, Vector3>>();

		static Tuple2<Vector3, Vector3> GetTangentPoints(ApexType type1, Vector3 center1, ApexType type2, Vector3 center2, float radius)
		{
			Tuple2<Vector3, Vector3> answer = null;
			if (type1 == ApexType.Point && type2 == ApexType.Point)
			{
				answer = new Tuple2<Vector3, Vector3>(center1, center2);
			}
			else if (type1 == ApexType.Point)
			{
				Vector3 tmp = MathUtility.GetTangent(center2, radius, center1, type2 == ApexType.Left);
				answer = new Tuple2<Vector3, Vector3>(center1, tmp);
			}
			else if (type2 == ApexType.Point)
			{
				Vector3 tmp = MathUtility.GetTangent(center1, radius, center2, type2 == ApexType.Right);
				answer = new Tuple2<Vector3, Vector3>(tmp, center2);
			}
			else if (type1 == type2)
			{
				answer = MathUtility.GetOutterTangent(center1, radius, center2, radius, type1 == ApexType.Left);
			}
			else
			{
				answer = MathUtility.GetInnerTangent(center1, radius, center2, radius, type1 == ApexType.Right);
			}

			tmpTangents.Add(answer);
			return answer;
		}
	}

	///<summary>
	// http://gamedev.stackexchange.com/questions/68302/how-does-the-simple-stupid-funnel-algorithm-work
	///</summary>
	internal static class PointObjectFunnel
	{
		static List<Vector3> Funnel(List<Vector3> portals)
		{
			Vector3 portalApex = portals[0];
			Vector3 portalLeft = portals[2];
			Vector3 portalRight = portals[3];

			List<Vector3> answer = new List<Vector3>(1 + portals.Count / 2);
			answer.Add(portalApex);

			int apexIndex = 0, leftIndex = 0, rightIndex = 0;
			int portalCount = portals.Count / 2;
			for (int i = 1; i < portalCount; ++i)
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

			answer.Add(portals[portals.Count - 1]);

			return answer;
		}
	}
}
