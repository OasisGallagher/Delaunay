using System.IO;
using System.Xml;

namespace Delaunay
{
	public class IDGenerator
	{
		public int Value
		{
			get { return Current++; }
		}

		public int Current { get; set; }

		public void WriteXml(XmlWriter writer)
		{
			writer.WriteAttributeString("IDCurrent", Current.ToString());
		}

		public void ReadXml(XmlReader reader)
		{
			Current = int.Parse(reader["IDCurrent"]);
		}

		public void WriteBinary(BinaryWriter writer)
		{
			writer.Write(Current);
		}

		public void ReadBinary(BinaryReader reader)
		{
			Current = reader.ReadInt32();
		}
	}
}
