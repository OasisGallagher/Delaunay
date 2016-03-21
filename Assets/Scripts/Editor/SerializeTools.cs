using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

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

	static class SerializeTools
	{
		public static void Save(string path, GeomManager geomManager)
		{
			if (!string.IsNullOrEmpty(path))
			{
				SaveXml(path, geomManager);
			}
		}

		public static void Load(string path, GeomManager geomManager)
		{
			if (!string.IsNullOrEmpty(path))
			{
				LoadXml(path, geomManager);
			}
		}

		static void LoadXml(string path, GeomManager geomManager)
		{
			XmlReaderSettings settings = new XmlReaderSettings();
			settings.IgnoreWhitespace = true;

			XmlReader reader = XmlReader.Create(path, settings);

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

			reader.Close();

			foreach (HalfEdge edge in container.Values)
			{
				geomManager._AddEdge(edge);
			}
		}

		static void SaveXml(string path, GeomManager geomManager)
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
			}

			writer.WriteEndDocument();
			writer.Close();
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
	}
}
