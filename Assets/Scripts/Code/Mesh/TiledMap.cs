﻿using System;
using System.Collections.Generic;
using UnityEngine;

namespace Delaunay
{
	/// <summary>
	/// 格子.
	/// </summary>
	public class Tile
	{
		/// <summary>
		/// 包含该格子中点的三角形.
		/// </summary>
		public Triangle Face;

		public override string ToString()
		{
			if (Face != null) { return Face.ToString(); }
			return base.ToString();
		}
	}

	/// <summary>
	/// 矩形区域.
	/// </summary>
	public struct Region
	{
		public int xMin, xMax;
		public int zMin, zMax;
	}

	/// <summary>
	/// 矩形区域, 由若干个格子组成.
	/// </summary>
	public struct TiledMapRegion
	{
		Region region;
		int x, z;
		public TiledMapRegion(TiledMap map, Region region)
		{
			this.region = region;
			x = region.xMin - 1;
			z = region.zMax;
		}

		/// <summary>
		/// 当前遍历的格子的索引.
		/// </summary>
		public Tuple2<int, int> Current
		{
			get { return new Tuple2<int, int>(x, z); }
		}

		/// <summary>
		/// 逐行遍历该区域内的格子.
		/// </summary>
		public bool MoveNext()
		{
			if (z++ >= region.zMax)
			{
				++x;
				z = region.zMin;
			}

			return x <= region.xMax;
		}
	}

	public class TiledMap
	{
		/// <summary>
		/// 方格的尺寸.
		/// </summary>
		public float TileSize
		{
			get { return tileSize; }
		}

		/// <summary>
		/// 行数.
		/// </summary>
		public int RowCount
		{
			get { return rowCount; }
		}

		/// <summary>
		/// 列数.
		/// </summary>
		public int ColumnCount
		{
			get { return columnCount; }
		}

		/// <summary>
		/// 起点.
		/// </summary>
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

		/// <summary>
		/// 获取第x行, 第z列的格子.
		/// </summary>
		public Tile this[int x, int z]
		{
			get
			{
				if (x >= rowCount || z >= columnCount)
				{
					throw new System.Exception("Index out of range");
				}
				return tiles[x, z];
			}
		}

		/// <summary>
		/// 获取坐标position所处的格子.
		/// </summary>
		public Tile this[Vector3 position]
		{
			get
			{
				position -= origin;
				position /= TileSize;

				int x = (int)position.x;
				int z = (int)position.z;
				if (x < 0 || z < 0 || x >= columnCount || z >= rowCount)
				{
					return null;
				}

				return tiles[z, x];
			}
		}

		/// <summary>
		/// 获取格子的中心点坐标.
		/// </summary>
		public Vector3 GetTileCenter(int x, int z)
		{
			return origin + new Vector3((x + 0.5f) * tileSize, 0f, (z + 0.5f) * tileSize);
		}

		/// <summary>
		/// 获取指定坐标区域的格子.
		/// </summary>
		public TiledMapRegion GetTiles(float xMin, float xMax, float zMin, float zMax)
		{
			xMin -= origin.x; xMax -= origin.x;
			zMin -= origin.z; zMax -= origin.z;

			xMin += tileSize / 2f; xMax -= tileSize / 2f;
			zMin += tileSize / 2f; zMax -= tileSize / 2f;

			xMin = Mathf.Clamp(xMin, 0, rowCount - 1);
			xMax = Mathf.Clamp(xMax, 0, rowCount - 1);
			zMin = Mathf.Clamp(zMin, 0, columnCount - 1);
			zMax = Mathf.Clamp(zMax, 0, columnCount - 1);

			Region region = new Region { xMin = (int)xMin, xMax = (int)xMax, zMin = (int)zMin, zMax = (int)zMax };
			return new TiledMapRegion(this, region);
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
