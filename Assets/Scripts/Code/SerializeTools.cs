using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Delaunay
{
	public static class SerializeTools
	{
		public static void Save(string path, GeomManager geomManager, List<Vector3> borderVertices)
		{
			if (!string.IsNullOrEmpty(path))
			{
				SaveBinary(path, geomManager, borderVertices);
			}
		}

		public static void Load(string path, GeomManager geomManager, List<Vector3> borderVertices)
		{
			if (!string.IsNullOrEmpty(path))
			{
				LoadBinary(path, geomManager, borderVertices);
			}
		}

		static void LoadBinary(string path, GeomManager geomManager, List<Vector3> borderVertices)
		{
			FileStream fs = new FileStream(path, FileMode.Open);
			BinaryReader reader = new BinaryReader(fs);

			borderVertices.Clear();
			int count = reader.ReadInt32();
			borderVertices.Capacity = count;
			for (int i = 0; i < count; ++i)
			{
				borderVertices.Add(reader.readVector3());
			}

			Vertex.VertexIDGenerator.ReadBinary(reader);
			count = reader.ReadInt32();

			for (int i = 0; i < count; ++i)
			{
				geomManager.CreateVertex(reader);
			}
			
			HalfEdge.HalfEdgeIDGenerator.ReadBinary(reader);

			List<Vertex> vertices = geomManager.AllVertices;
			Dictionary<int, HalfEdge> container = new Dictionary<int, HalfEdge>();
			count = reader.ReadInt32();
			for (int i = 0; i < count; ++i)
			{
				geomManager.CreateEdge(reader, vertices, container);
			}

			Triangle.TriangleIDGenerator.ReadBinary(reader);
			count = reader.ReadInt32();
			for (int i = 0; i < count; ++i)
			{
				geomManager.CreateTriangle(reader, container);
			}

			Obstacle.ObstacleIDGenerator.ReadBinary(reader);
			count = reader.ReadInt32();
			for (int i = 0; i < count; ++i)
			{
				geomManager.CreateObstacle(reader, container);
			}

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
				geomManager._AddEdge(edge);
			}
		}

		static void SaveBinary(string path, GeomManager geomManager, List<Vector3> borderVertices)
		{
			FileStream fs = new FileStream(path, FileMode.Create);
			BinaryWriter writer = new BinaryWriter(fs);

			writer.Write(borderVertices.Count);
			borderVertices.ForEach(item => { writer.write(item); });

			Vertex.VertexIDGenerator.WriteBinary(writer);
			writer.Write(geomManager.AllVertices.Count);
			geomManager.AllVertices.ForEach(item => { item.WriteBinary(writer); });

			HalfEdge.HalfEdgeIDGenerator.WriteBinary(writer);
			writer.Write(geomManager.AllEdges.Count);
			geomManager.AllEdges.ForEach(item => { item.WriteBinary(writer); });

			Triangle.TriangleIDGenerator.WriteBinary(writer);
			writer.Write(geomManager.AllTriangles.Count);
			geomManager.AllTriangles.ForEach(item => { item.WriteBinary(writer); });

			Obstacle.ObstacleIDGenerator.WriteBinary(writer);
			writer.Write(geomManager.AllObstacles.Count);
			geomManager.AllObstacles.ForEach(item => { item.WriteBinary(writer); });

			BorderSet.BorderSetIDGenerator.WriteBinary(writer);
			writer.Write(geomManager.AllBorderSets.Count);
			geomManager.AllBorderSets.ForEach(item => { item.WriteBinary(writer); });

			writer.Close();
			fs.Close();
		}
	}
}
