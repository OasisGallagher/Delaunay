using System.IO;

namespace Delaunay
{
	public class IDGenerator
	{
		public int Value
		{
			get { return Current++; }
		}

		public int Current { get; set; }

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
