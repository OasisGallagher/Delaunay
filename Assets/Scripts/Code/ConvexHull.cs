using System.Collections.Generic;
using UnityEngine;

namespace Delaunay
{
	public static class ConvexHullComputer
	{
		class VertexStack
		{
			List<Vector3> container = new List<Vector3>();
			public Vector3 Pop()
			{
				Vector3 result = container[container.Count - 1];
				container.RemoveAt(container.Count - 1);
				return result;
			}

			public void Push(Vector3 u)
			{
				container.Add(u);
			}

			public Vector3 Peek(int index = 0)
			{
				return container[container.Count - 1 - index];
			}

			public List<Vector3> Container { get { return new List<Vector3>(container); } }
		}

		static Vector3 PopLowestVertex(List<Vector3> vertices)
		{
			float minZ = vertices[0].z;
			int index = 0;
			for (int i = 1; i < vertices.Count; ++i)
			{
				if (vertices[i].z < minZ)
				{
					index = i;
					minZ = vertices[i].z;
				}
			}

			Vector3 result = vertices[index];
			vertices[index] = vertices[vertices.Count - 1];
			vertices.RemoveAt(vertices.Count - 1);
			return result;
		}

		class ConvexHullVertexComparer : IComparer<Vector3>
		{
			Vector3 start = Vector3.zero;
			public ConvexHullVertexComparer(Vector3 o)
			{
				start = o;
			}

			int IComparer<Vector3>.Compare(Vector3 lhs, Vector3 rhs)
			{
				bool b1 = (lhs - start).cross2(new Vector3(1, 0, 0)) > 0;
				bool b2 = (rhs - start).cross2(new Vector3(1, 0, 0)) > 0;

				if (b1 != b2) { return b2 ? -1 : 1; }

				float c = (lhs - start).cross2(rhs - start);
				if (!Mathf.Approximately(c, 0)) { return c > 0 ? -1 : 1; }

				Vector3 drhs = rhs - start;
				Vector3 dlhs = lhs - start;
				drhs.y = dlhs.y = 0f;

				return (int)Mathf.Sign(drhs.sqrMagnitude2() - dlhs.sqrMagnitude2());
			}
		}

		public static List<Vector3> Compute(List<Vector3> vertices)
		{
			if (vertices.Count <= 3) { return vertices; }

			Vector3 p0 = PopLowestVertex(vertices);
			vertices.Sort(new ConvexHullVertexComparer(p0));

			VertexStack stack = new VertexStack();
			stack.Push(p0);
			stack.Push(vertices[0]);
			stack.Push(vertices[1]);

			for (int i = 2; i < vertices.Count; ++i)
			{
				Vector3 pi = vertices[i];
				for (; ; )
				{
					Utility.Verify(stack.Container.Count > 0);

					Vector3 top = stack.Peek(), next2top = stack.Peek(1);
					float cr = (next2top - top).cross2(pi - top);

					if (cr <= 0) { break; }

					stack.Pop();
				}

				stack.Push(pi);
			}

			return stack.Container;
		}
	}
}
