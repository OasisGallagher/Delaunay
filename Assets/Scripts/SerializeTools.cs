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
			if (string.IsNullOrEmpty(path)) { return; }

			File.Delete(path);

			XmlWriterSettings settings = new XmlWriterSettings();
			settings.Indent = true;
			settings.Encoding = new UTF8Encoding(false);

			settings.NewLineChars = Environment.NewLine;
			XmlWriter writer = XmlWriter.Create(path, settings);

			writer.WriteStartDocument();
			using (new XmlWriterScope(writer, "Root"))
			{
				using (new XmlWriterScope(writer, "Vertices"))
				{
					WriteAllVertices(writer);
				}

				using (new XmlWriterScope(writer, "Edges"))
				{
					WriteAllEdges(writer);
				}
			}

			writer.WriteEndDocument();
			writer.Close();
		}

		public static void Load(string path)
		{
			XmlReader reader = XmlReader.Create(path);
			for (; reader.Read(); )
			{
				if (reader.NodeType != XmlNodeType.Element) { continue; }
				if (reader.Name == EditorConstants.kXmlVertex)
				{
					Vertex.Create(reader);
				}
				else if (reader.Name == EditorConstants.kXmlEdge)
				{ 

				}
			}
		}

		static void WriteAllVertices(XmlWriter writer)
		{
			GeomManager.SortedVertices.ForEach(vertex =>
			{
				using (new XmlWriterScope(writer, "Vertex")) { vertex.WriteXml(writer); }
			});
		}

		static void WriteAllEdges(XmlWriter writer)
		{
			GeomManager.SortedVertices.ForEach(vertex =>
			{
				GeomManager.GetRays(vertex).ForEach(edge =>
				{
					using (new XmlWriterScope(writer, "Edge")) { edge.WriteXml(writer); }
				});
			});
		}
	}
}
