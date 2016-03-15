using UnityEngine;

namespace Delaunay
{
	public class Tile
	{
		public Triangle Face;
	}

	public class TiledMap
	{
		public float TileSize
		{
			get { return tileSize; }
		}

		public int RowCount
		{
			get { return rowCount; }
		}

		public int ColumnCount
		{
			get { return columnCount; }
		}

		public Vector3 Origin
		{
			get { return origin; }
		}

		Tile[,] tiles = null;
		int rowCount, columnCount;
		Vector3 origin;
		float tileSize;

		public TiledMap(Vector3 origin, float tileSize, int rowCount, int columnCount)
		{
			this.rowCount = rowCount;
			this.columnCount = columnCount;
			this.tileSize = tileSize;
			this.origin = origin;

			InitTiles(tileSize, rowCount, columnCount);
		}

		public Tile GetTile(Vector3 position)
		{
			position -= origin;
			position /= TileSize;
			int x = Mathf.FloorToInt(position.x);
			int z = Mathf.FloorToInt(position.z);
			if (x > columnCount || z > rowCount)
			{
				return null;
			}

			return tiles[x, z];
		}

		void InitTiles(float size, int rowCount, int columnCount)
		{
			tiles = new Tile[rowCount, columnCount];
			for (int i = 0; i < rowCount; ++i)
			{
				for (int j = 0; j < columnCount; ++j)
				{
					tiles[i, j] = new Tile();
				}
			}
		}
	}
}
