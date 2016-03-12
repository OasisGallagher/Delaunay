using UnityEngine;

namespace Delaunay
{
	public class Tile
	{
		public Triangle Facet;
	}

	public class TiledMap
	{
		Tile[,] tiles = null;
		float tileSize;
		int rowCount, columnCount;
		public TiledMap(float size, int nRow, int nCol)
		{
			InitTiles(size, nRow, nCol);
		}

		void InitTiles(float size, int nRow, int nCol)
		{
			tiles = new Tile[nRow, nCol];
			for (int i = 0; i < nRow; ++i)
			{
				for (int j = 0; j < nCol; ++j)
				{
					tiles[i, j] = new Tile();
				}
			}

			tileSize = size;

		}

		Tile GetTile(Vector3 position)
		{
			return null;
		}
	}
}
