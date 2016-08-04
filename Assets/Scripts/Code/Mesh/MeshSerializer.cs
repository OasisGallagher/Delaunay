using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Delaunay
{
	public static class MeshSerializer
	{
		/// <summary>
		/// 保存网格文件.
		/// </summary>
		public static void Save(string path, GeomManager geomManager, List<Vector3> borderVertices)
		{
			if (!string.IsNullOrEmpty(path))
			{
				SaveBinary(path, geomManager, borderVertices);
			}
		}

		/// <summary>
		/// 加载网格文件.
		/// </summary>
		public static void Load(string path, GeomManager geomManager, List<Vector3> borderVertices)
		{
			if (!string.IsNullOrEmpty(path))
			{
				LoadBinary(path, geomManager, borderVertices);
			}
		}

		/// <summary>
		/// 加载管理器和边框.
		/// </summary>
		static void LoadBinary(string path, GeomManager geomManager, List<Vector3> borderVertices)
		{
			FileStream fs = new FileStream(path, FileMode.Open);
			BinaryReader reader = new BinaryReader(fs);

			// 加载边框.
			borderVertices.Clear();
			int count = reader.ReadInt32();
			borderVertices.Capacity = count;
			for (int i = 0; i < count; ++i)
			{
				borderVertices.Add(reader.readVector3());
			}

			// 加载顶点.
			Vertex.VertexIDGenerator.ReadBinary(reader);
			count = reader.ReadInt32();

			for (int i = 0; i < count; ++i)
			{
				geomManager.CreateVertex(reader);
			}

			// 加载边.
			HalfEdge.HalfEdgeIDGenerator.ReadBinary(reader);

			List<Vertex> vertices = geomManager.AllVertices;
			Dictionary<int, HalfEdge> container = new Dictionary<int, HalfEdge>();
			count = reader.ReadInt32();
			for (int i = 0; i < count; ++i)
			{
				geomManager.CreateEdge(reader, vertices, container);
			}

			// 加载三角形.
			Triangle.TriangleIDGenerator.ReadBinary(reader);
			count = reader.ReadInt32();
			for (int i = 0; i < count; ++i)
			{
				geomManager.CreateTriangle(reader, container);
			}

			// 加载障碍物.
			Obstacle.ObstacleIDGenerator.ReadBinary(reader);
			count = reader.ReadInt32();
			for (int i = 0; i < count; ++i)
			{
				geomManager.CreateObstacle(reader, container);
			}

			// 加载边集.
			BorderSet.BorderSetIDGenerator.ReadBinary(reader);
			count = reader.ReadInt32();
			for (int i = 0; i < count; ++i)
			{
				geomManager.CreateBorderSet(reader, container);
			}

			reader.Close();
			fs.Close();

			foreach (HalfEdge edge in container.Values)
			{
				geomManager.AddUnserializedEdge(edge);
			}
		}

		/// <summary>
		/// 保存管理器和边框.
		/// </summary>
		static void SaveBinary(string path, GeomManager geomManager, List<Vector3> borderVertices)
		{
			FileStream fs = new FileStream(path, FileMode.Create);
			BinaryWriter writer = new BinaryWriter(fs);

			// 保存边框.
			writer.Write(borderVertices.Count);
			borderVertices.ForEach(item => { writer.write(item); });

			// 保存顶点.
			Vertex.VertexIDGenerator.WriteBinary(writer);
			writer.Write(geomManager.AllVertices.Count);
			geomManager.AllVertices.ForEach(item => { item.WriteBinary(writer); });

			// 保存边.
			HalfEdge.HalfEdgeIDGenerator.WriteBinary(writer);
			writer.Write(geomManager.AllEdges.Count);
			geomManager.AllEdges.ForEach(item => { item.WriteBinary(writer); });

			// 保存三角形.
			Triangle.TriangleIDGenerator.WriteBinary(writer);
			writer.Write(geomManager.AllTriangles.Count);
			geomManager.AllTriangles.ForEach(item => { item.WriteBinary(writer); });

			// 保存障碍物.
			Obstacle.ObstacleIDGenerator.WriteBinary(writer);
			writer.Write(geomManager.AllObstacles.Count);
			geomManager.AllObstacles.ForEach(item => { item.WriteBinary(writer); });

			// 保存边集.
			BorderSet.BorderSetIDGenerator.WriteBinary(writer);
			writer.Write(geomManager.AllBorderSets.Count);
			geomManager.AllBorderSets.ForEach(item => { item.WriteBinary(writer); });

			writer.Close();
			fs.Close();
		}
	}
}
