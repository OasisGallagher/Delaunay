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
			sequence[index++].PlayForward();
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
			get { return index < sequence.Count; }
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
		IEnumerable<Vector3> vertices;
		int borderSetID;

		public CreateBorderSetCommand(DelaunayMesh mesh, IEnumerable<Vector3> vertices)
		{
			this.mesh = mesh;
			this.vertices = vertices;
		}

		public void PlayForward()
		{
			borderSetID = mesh.AddBorderSet(vertices).ID;
		}

		public void PlayReverse()
		{
			Utility.Verify(borderSetID >= 0);
			mesh.RemoveBorderSet(borderSetID);
		}
	}

	public class CreateObstacleCommand : IEditCommand
	{
		DelaunayMesh mesh;
		IEnumerable<Vector3> vertices;
		int obstacleID;

		public CreateObstacleCommand(DelaunayMesh mesh, IEnumerable<Vector3> vertices)
		{
			this.mesh = mesh;
			this.vertices = vertices;
		}

		public void PlayForward()
		{
			obstacleID = mesh.AddObstacle(vertices).ID;
		}

		public void PlayReverse()
		{
			Utility.Verify(obstacleID >= 0);
			mesh.RemoveObstacle(obstacleID);
		}
	}
}