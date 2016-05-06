using System.Collections.Generic;
using UnityEngine;

namespace Delaunay
{
	public interface IEditCommand
	{
		void PlayForward();
		void PlayReverse();
	}

	public class EditCommandSequence
	{
		int index = -1;
		List<IEditCommand> sequence = new List<IEditCommand>();

		public void Push(IEditCommand item)
		{
			sequence.RemoveRange(index + 1, sequence.Count - (index + 1));

			item.PlayForward();
			sequence.Add(item);

			index = sequence.Count - 1;
		}

		public void Undo()
		{
			Utility.Verify(CanUndo);
			sequence[index--].PlayReverse();
		}

		public void Redo()
		{
			Utility.Verify(CanRedo);
			sequence[++index].PlayForward();
		}

		public void Clear()
		{
			sequence.Clear();
			index = -1;
		}

		public bool CanUndo
		{
			get { return index >= 0; }
		}

		public bool CanRedo
		{
			get { return index < sequence.Count - 1; }
		}
	}

	public class AddVertexCommand : IEditCommand
	{
		List<Vector3> target;
		Vector3 parameter;

		public AddVertexCommand(List<Vector3> target, Vector3 parameter)
		{
			this.target = target;
			this.parameter = parameter;
		}

		public void PlayForward()
		{
			this.target.Add(parameter);
		}

		public void PlayReverse()
		{
			for (int i = target.Count - 1; i >= 0; --i)
			{
				if (parameter == target[i])
				{
					target.RemoveAt(i);
					break;
				}
			}
		}
	}

	public class MoveVertexCommand : IEditCommand
	{
		int index;
		Vector3 newPosition, oldPosition;
		List<Vector3> target;

		public MoveVertexCommand(List<Vector3> target, int index, Vector3 newPosition)
		{
			this.index = index;
			this.target = target;
			this.newPosition = newPosition;
			this.oldPosition = target[index];
		}

		public void PlayForward()
		{
			target[index] = newPosition;
		}

		public void PlayReverse()
		{
			target[index] = oldPosition;
		}
	}

	public class CreateBorderSetCommand : IEditCommand
	{
		DelaunayMesh mesh;
		List<Vector3> refVertices;
		List<Vector3> savedVertices;

		int borderSetID;
		bool close;

		public CreateBorderSetCommand(List<Vector3> vertices, DelaunayMesh mesh, bool close)
		{
			this.mesh = mesh;
			this.refVertices = vertices;
			this.savedVertices = new List<Vector3>(vertices);
			this.close = close;
		}

		public void PlayForward()
		{
			borderSetID = mesh.AddBorderSet(savedVertices, close).ID;
			refVertices.Clear();
		}

		public void PlayReverse()
		{
			Utility.Verify(borderSetID >= 0);
			mesh.RemoveBorderSet(borderSetID);
			refVertices.Clear();
			refVertices.AddRange(savedVertices);
		}
	}

	public class CreateSuperBorderCommand : IEditCommand
	{
		DelaunayMesh mesh;
		List<Vector3> savedVertices;
		List<Vector3> refVertices;

		public CreateSuperBorderCommand(List<Vector3> vertices, DelaunayMesh mesh)
		{
			this.mesh = mesh;
			this.refVertices = vertices;
			this.savedVertices = new List<Vector3>(vertices);
		}

		public void PlayForward()
		{
			mesh.AddSuperBorder(savedVertices);
			refVertices.Clear();
		}

		public void PlayReverse()
		{
			mesh.ClearAll();
			refVertices.Clear();
			refVertices.AddRange(savedVertices);
		}
	}

	public class CreateObstacleCommand : IEditCommand
	{
		DelaunayMesh mesh;
		List<Vector3> refVertices;
		List<Vector3> savedVertices;

		int obstacleID;

		public CreateObstacleCommand(List<Vector3> vertices, DelaunayMesh mesh)
		{
			this.mesh = mesh;
			this.refVertices = vertices;
			this.savedVertices = new List<Vector3>(vertices);
		}

		public void PlayForward()
		{
			obstacleID = mesh.AddObstacle(savedVertices).ID;
			refVertices.Clear();
		}

		public void PlayReverse()
		{
			Utility.Verify(obstacleID >= 0);
			mesh.RemoveObstacle(obstacleID);
			refVertices.Clear();
			refVertices.AddRange(savedVertices);
		}
	}
}