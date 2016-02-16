using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace Delaunay
{
	static class SerializeTools
	{
		public static void Save(string path)
		{
			if (!string.IsNullOrEmpty(path))
			{
				SaveXml(path);
			}
		}

		public static void Load(string path)
		{
			if (!string.IsNullOrEmpty(path))
			{
				LoadXml(path);
			}
		}

		static void LoadXml(string path)
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
				if (reader.Name == EditorConstants.kXmlVertex)
				{
					Vertex.Create(reader);
				}
			}

			Dictionary<int, HalfEdge> container = new Dictionary<int, HalfEdge>();
			for (; reader.Read(); )
			{
				if (reader.NodeType == XmlNodeType.EndElement
					&& reader.Name == EditorConstants.kXmlAllEdges)
				{
					break;
				}

				if (reader.NodeType != XmlNodeType.Element) { continue; }
				if (reader.Name == EditorConstants.kXmlEdge)
				{
					HalfEdge.Create(reader, container);
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
				if (reader.Name == EditorConstants.kXmlTriangle)
				{
					Triangle.Create(reader, container);
				}
			}

			reader.Close();

			foreach (HalfEdge edge in container.Values)
			{
				GeomManager.AddEdge(edge);
			}
		}

		static void SaveXml(string path)
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
					WriteAllVertices(writer);
				}

				using (new XmlWriterScope(writer, EditorConstants.kXmlAllEdges))
				{
					WriteAllEdges(writer);
				}

				using (new XmlWriterScope(writer, EditorConstants.kXmlAllTriangles))
				{
					WriteAllTriangles(writer);
				}
			}

			writer.WriteEndDocument();
			writer.Close();
		}

		static void WriteAllVertices(XmlWriter writer)
		{
			GeomManager.AllVertices.ForEach(vertex =>
			{
				using (new XmlWriterScope(writer, EditorConstants.kXmlVertex))
				{
					vertex.WriteXml(writer);
				}
			});
		}

		static void WriteAllEdges(XmlWriter writer)
		{
			GeomManager.AllEdges.ForEach(edge =>
			{
				using (new XmlWriterScope(writer, EditorConstants.kXmlEdge))
				{
					edge.WriteXml(writer);
				}
			});
		}

		static void WriteAllTriangles(XmlWriter writer)
		{
			GeomManager.AllTriangles.ForEach(triangle =>
			{
				using (new XmlWriterScope(writer, EditorConstants.kXmlTriangle))
				{
					triangle.WriteXml(writer);
				}
			});
		}
	}
}
