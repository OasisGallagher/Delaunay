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
			XmlWriterSettings settings = new XmlWriterSettings();
			settings.Indent = true;
			settings.Encoding = new UTF8Encoding(false);

			settings.NewLineChars = Environment.NewLine;
			XmlWriter writer = XmlWriter.Create(path, settings);

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
		}

		private static void WriteAllVertices(XmlWriter writer)
		{
			foreach (Vertex vertex in GeomManager.SortedVertices)
			{
				using (new XmlWriterScope(writer, "Vertex")) { vertex.WriteXml(writer); }
			}
		}

		private static void WriteAllEdges(XmlWriter writer)
		{
			foreach (Vertex vertex in GeomManager.SortedVertices)
			{
				foreach (HalfEdge edge in GeomManager.GetRays(vertex))
				{
					using (new XmlWriterScope(writer, "Edge")) { edge.WriteXml(writer); }
				}
			}
		}

		static void WriteVertex(Vertex vertex, XmlElement element)
		{
			element.SetAttribute("ID", vertex.ID.ToString());
			element.SetAttribute("EdgeID", vertex.Edge.ID.ToString());
			element.SetAttribute("X", vertex.Position.x.ToString());
			element.SetAttribute("Y", vertex.Position.y.ToString());
			element.SetAttribute("Z", vertex.Position.z.ToString());
		}

		static void WriteEdge(HalfEdge edge, XmlElement element)
		{
			element.SetAttribute("ID", edge.ID.ToString());
			element.SetAttribute("DestVertexID", edge.Dest.ID.ToString());
			element.SetAttribute("NextEdgeID", edge.Next.ID.ToString());
			element.SetAttribute("PairEdgeID", edge.Pair.ID.ToString());
			element.SetAttribute("FaceID", edge.Face != null ? edge.Face.ID.ToString() : "-1");
		}
	}
}
