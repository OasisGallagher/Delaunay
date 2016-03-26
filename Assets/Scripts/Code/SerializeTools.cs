using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using UnityEngine;

namespace Delaunay
{
	public class XmlWriterScope : IDisposable
	{
		XmlWriter writer = null;
		public XmlWriterScope(XmlWriter writer, string localName)
		{
			this.writer = writer;
			writer.WriteStartElement(localName);
		}

		public void Dispose() { writer.WriteEndElement(); }
	}

	public static class SerializeTools
	{
		public static void Save(string path, GeomManager geomManager, List<Vector3> borderVertices)
		{
			if (!string.IsNullOrEmpty(path))
			{
				SaveXml(path, geomManager, borderVertices);
			}
		}

		public static void Load(string path, GeomManager geomManager, List<Vector3> borderVertices)
		{
			if (!string.IsNullOrEmpty(path))
			{
				LoadXml(path, geomManager, borderVertices);
			}
		}

		static void LoadXml(string path, GeomManager geomManager, List<Vector3> borderVertices)
		{
			XmlReaderSettings settings = new XmlReaderSettings();
			settings.IgnoreWhitespace = true;

			XmlReader reader = XmlReader.Create(path, settings);

			borderVertices.Clear();
			for (; reader.Read(); )
			{
				if (reader.NodeType == XmlNodeType.EndElement
					&& reader.Name == EditorConstants.kXmlAllBorderVertices)
				{
					break;
				}

				if (reader.NodeType != XmlNodeType.Element) { continue; }

				if (reader.Name == EditorConstants.kXmlBorderVertex)
				{
					borderVertices.Add(new Vector3(float.Parse(reader["X"]), float.Parse(reader["Y"]), float.Parse(reader["Z"])));
				}
			}

			for (; reader.Read(); )
			{
				if (reader.NodeType == XmlNodeType.EndElement
					&& reader.Name == EditorConstants.kXmlAllVertices)
				{
					break;
				}

				if (reader.NodeType != XmlNodeType.Element) { continue; }

				if (reader.Name == EditorConstants.kXmlAllVertices)
				{
					Vertex.VertexIDGenerator.ReadXml(reader);
				}

				if (reader.Name == EditorConstants.kXmlVertex)
				{
					geomManager.CreateVertex(reader);
				}
			}

			List<Vertex> vertices = geomManager.AllVertices;

			Dictionary<int, HalfEdge> container = new Dictionary<int, HalfEdge>();
			for (; reader.Read(); )
			{
				if (reader.NodeType == XmlNodeType.EndElement
					&& reader.Name == EditorConstants.kXmlAllEdges)
				{
					break;
				}

				if (reader.NodeType != XmlNodeType.Element) { continue; }

				if (reader.Name == EditorConstants.kXmlAllEdges)
				{
					HalfEdge.HalfEdgeIDGenerator.ReadXml(reader);
				}

				if (reader.Name == EditorConstants.kXmlEdge)
				{
					geomManager.CreateEdge(reader, vertices, container);
				}
			}

			for (; reader.Read(); )
			{
				if (reader.NodeType == XmlNodeType.EndElement
					&& reader.Name == EditorConstants.kXmlAllTriangles)
				{
					break;
				}

				if (reader.NodeType != XmlNodeType.Element) { continue; }

				if (reader.Name == EditorConstants.kXmlAllTriangles)
				{
					Triangle.TriangleIDGenerator.ReadXml(reader);
				}

				if (reader.Name == EditorConstants.kXmlTriangle)
				{
					geomManager.CreateTriangle(reader, container);
				}
			}

			for (; reader.Read(); )
			{
				if (reader.NodeType == XmlNodeType.EndElement
					&& reader.Name == EditorConstants.kXmlAllObstacles)
				{
					break;
				}

				if (reader.NodeType != XmlNodeType.Element) { continue; }

				if (reader.Name == EditorConstants.kXmlAllObstacles)
				{
					Obstacle.ObstacleIDGenerator.ReadXml(reader);
				}

				if (reader.Name == EditorConstants.kXmlObstacle)
				{
					geomManager.CreateObstacle(reader, container);
				}
			}

			for (; reader.Read(); )
			{
				if (reader.NodeType == XmlNodeType.EndElement
					&& reader.Name == EditorConstants.kXmlAllBorderClusters)
				{
					break;
				}

				if (reader.NodeType != XmlNodeType.Element) { continue; }

				if (reader.Name == EditorConstants.kXmlAllBorderClusters)
				{
					BorderCluster.BorderClusterIDGenerator.ReadXml(reader);
				}

				if (reader.Name == EditorConstants.kXmlBorderCluster)
				{
					geomManager.CreateBorderCluster(reader, container);
				}
			}

			reader.Close();

			foreach (HalfEdge edge in container.Values)
			{
				geomManager._AddEdge(edge);
			}
		}

		static void SaveXml(string path, GeomManager geomManager, List<Vector3> borderVertices)
		{
			File.Delete(path);

			XmlWriterSettings settings = new XmlWriterSettings();
			settings.Indent = true;
			settings.Encoding = new UTF8Encoding(false);

			settings.NewLineChars = Environment.NewLine;
			XmlWriter writer = XmlWriter.Create(path, settings);

			writer.WriteStartDocument();
			using (new XmlWriterScope(writer, EditorConstants.kXmlRoot))
			{
				using (new XmlWriterScope(writer, EditorConstants.kXmlAllBorderVertices))
				{
					WriteAllBorderVertices(writer, borderVertices);
				}

				using (new XmlWriterScope(writer, EditorConstants.kXmlAllVertices))
				{
					Vertex.VertexIDGenerator.WriteXml(writer);
					WriteAllVertices(writer, geomManager);
				}

				using (new XmlWriterScope(writer, EditorConstants.kXmlAllEdges))
				{
					HalfEdge.HalfEdgeIDGenerator.WriteXml(writer);
					WriteAllEdges(writer, geomManager);
				}

				using (new XmlWriterScope(writer, EditorConstants.kXmlAllTriangles))
				{
					Triangle.TriangleIDGenerator.WriteXml(writer);
					WriteAllTriangles(writer, geomManager);
				}

				using (new XmlWriterScope(writer, EditorConstants.kXmlAllObstacles))
				{
					Obstacle.ObstacleIDGenerator.WriteXml(writer);
					WriteAllObstacles(writer, geomManager);
				}

				using (new XmlWriterScope(writer, EditorConstants.kXmlAllBorderClusters))
				{
					BorderCluster.BorderClusterIDGenerator.WriteXml(writer);
					WriteAllBorderClusters(writer, geomManager);
				}
			}

			writer.WriteEndDocument();
			writer.Close();
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

			BorderCluster.BorderClusterIDGenerator.ReadBinary(reader);
			count = reader.ReadInt32();
			for (int i = 0; i < count; ++i)
			{
				geomManager.CreateBorderCluster(reader, container);
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
			FileStream fs = new FileStream(path, FileMode.Truncate);
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

			BorderCluster.BorderClusterIDGenerator.WriteBinary(writer);
			writer.Write(geomManager.AllBorderClusters.Count);
			geomManager.AllBorderClusters.ForEach(item => { item.WriteBinary(writer); });

			writer.Close();
			fs.Close();
		}

		static void WriteAllBorderVertices(XmlWriter writer, List<Vector3> borderVertices)
		{
			borderVertices.ForEach(position =>
			{
				using (new XmlWriterScope(writer, EditorConstants.kXmlBorderVertex))
				{
					writer.WriteAttributeString("X", position.x.ToString());
					writer.WriteAttributeString("Y", position.y.ToString());
					writer.WriteAttributeString("Z", position.z.ToString());
				}
			});
		}

		static void WriteAllVertices(XmlWriter writer, GeomManager geomManager)
		{
			geomManager.AllVertices.ForEach(vertex =>
			{
				using (new XmlWriterScope(writer, EditorConstants.kXmlVertex))
				{
					vertex.WriteXml(writer);
				}
			});
		}

		static void WriteAllEdges(XmlWriter writer, GeomManager geomManager)
		{
			geomManager.AllEdges.ForEach(edge =>
			{
				using (new XmlWriterScope(writer, EditorConstants.kXmlEdge))
				{
					edge.WriteXml(writer);
				}
			});
		}

		static void WriteAllTriangles(XmlWriter writer, GeomManager geomManager)
		{
			geomManager.AllTriangles.ForEach(triangle =>
			{
				using (new XmlWriterScope(writer, EditorConstants.kXmlTriangle))
				{
					triangle.WriteXml(writer);
				}
			});
		}

		static void WriteAllObstacles(XmlWriter writer, GeomManager geomManager)
		{
			geomManager.AllObstacles.ForEach(obstacle =>
			{
				using (new XmlWriterScope(writer, EditorConstants.kXmlObstacle))
				{
					obstacle.WriteXml(writer);
				}
			});
		}

		static void WriteAllBorderClusters(XmlWriter writer, GeomManager geomManager)
		{
			geomManager.AllBorderClusters.ForEach(borderCluster =>
			{
				using (new XmlWriterScope(writer, EditorConstants.kXmlBorderCluster))
				{
					borderCluster.WriteXml(writer);
				}
			});
		}
	}
}
