using System;
using System.Collections.Generic;
using UnityEngine;

namespace Delaunay
{
	/// <summary>
	/// 编辑命令, 用于撤销和重做.
	/// </summary>
	public interface IEditCommand
	{
		void PlayForward();
		void PlayReverse();
	}

	/// <summary>
	/// 编辑命令序列.
	/// </summary>
	public class EditCommandSequence
	{
		int index = -1;
		List<IEditCommand> sequence = new List<IEditCommand>();

		/// <summary>
		/// 加入一个编辑命令, 并正向运行.
		/// </summary>
		public void Push(IEditCommand item)
		{
			sequence.RemoveRange(index + 1, sequence.Count - (index + 1));

			item.PlayForward();
			sequence.Add(item);

			index = sequence.Count - 1;
		}

		/// <summary>
		/// 撤销.
		/// </summary>
		public void Undo()
		{
			Utility.Verify(CanUndo);
			sequence[index--].PlayReverse();
		}

		/// <summary>
		/// 重做.
		/// </summary>
		public void Redo()
		{
			Utility.Verify(CanRedo);
			sequence[++index].PlayForward();
		}

		/// <summary>
		/// 清空命令序列.
		/// </summary>
		public void Clear()
		{
			sequence.Clear();
			index = -1;
		}

		/// <summary>
		/// 是否可以撤销(即序列内是否存在上一个命令).
		/// </summary>
		public bool CanUndo
		{
			get { return index >= 0; }
		}

		/// <summary>
		/// 是否可以重做(即序列内是否存在下一个命令).
		/// </summary>
		public bool CanRedo
		{
			get { return index < sequence.Count - 1; }
		}
	}

	/// <summary>
	/// 添加节点命令.
	/// </summary>
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

	/// <summary>
	/// 移动节点命令.
	/// </summary>
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

	/// <summary>
	/// 创建边集.
	/// </summary>
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

	/// <summary>
	/// 创建超级边框.
	/// </summary>
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

	/// <summary>
	/// 创建障碍物.
	/// </summary>
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

	/// <summary>
	/// 创建障碍物, 创建过程以动画的形势展示.
	/// </summary>
	public class CreateObstacleAnimatedCommand : IEditCommand
	{
		AnimatedDelaunayMesh mesh;
		List<Vector3> refVertices;
		List<Vector3> savedVertices;
		Action<Obstacle> onCreate;

		int obstacleID;

		public CreateObstacleAnimatedCommand(List<Vector3> vertices, AnimatedDelaunayMesh mesh, Action<Obstacle> onCreate)
		{
			this.mesh = mesh;
			this.refVertices = vertices;
			this.savedVertices = new List<Vector3>(vertices);
			this.onCreate = onCreate;
		}

		public void PlayForward()
		{
			mesh.AnimatedAddObstacle(savedVertices, (obstacle) =>
			{
				if (obstacle != null) { obstacleID = obstacle.ID; }
				if (onCreate != null) { onCreate(obstacle); }
			});

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