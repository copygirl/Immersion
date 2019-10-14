using System;
using System.Collections;
using System.Collections.Generic;
using Godot;

namespace Immersion.Voxel
{
	public class TextureAtlas<T> : IEnumerable<T>
	{
		private readonly Dictionary<T, TextureCell> _mappings = new Dictionary<T, TextureCell>();
		
		public int Width { get; }
		public int Height { get; }
		public int CellSize { get; }
		
		public int Count => _mappings.Count;
		
		public TextureCell this[T key] { get {
			if (key == null) throw new ArgumentNullException(nameof(key));
			if (_mappings.TryGetValue(key, out TextureCell cell)) return cell;
				throw new KeyNotFoundException($"Key '{key}' was not found in texture mapping");
		} }
		
		public TextureAtlas(int width, int height, int cellSize)
		{
			if (width <= 0) throw new ArgumentOutOfRangeException(
				nameof(width), width, $"{nameof(width)} (={width}) is too small");
			if (height <= 0) throw new ArgumentOutOfRangeException(
				nameof(height), height, $"{nameof(height)} (={height}) is too small");
			
			// Make sure an even number of cells fit into the specified width and height.
			if ((width % cellSize) != 0) throw new ArgumentException(
				$"{nameof(width)} (={width}) is not divisible by {nameof(cellSize)} (={cellSize})");
			if ((height % cellSize) != 0) throw new ArgumentException(
				$"{nameof(height)} (={height}) is not divisible by {nameof(cellSize)} (={cellSize})");
			
			Width    = width;
			Height   = height;
			CellSize = cellSize;
		}
		
		public void Add(T key, int x, int y)
		{
			if (key == null) throw new ArgumentNullException(nameof(key));
			if ((x < 0) || (x >= Width / CellSize)) throw new ArgumentOutOfRangeException(
				nameof(x), x, $"{nameof(x)} (={x}) is not inside range [0, {Width / CellSize})");
			if ((y < 0) || (y >= Height / CellSize)) throw new ArgumentOutOfRangeException(
				nameof(y), y, $"{nameof(y)} (={y}) is not inside range [0, {Height / CellSize})");
			
			_mappings.Add(key, new TextureCell(
				(float)( x      * CellSize) / Width  + 0.001F,
				(float)( y      * CellSize) / Height + 0.001F,
				(float)((x + 1) * CellSize) / Width  - 0.001F,
				(float)((y + 1) * CellSize) / Height - 0.001F));
		}
		
		public IEnumerator<T> GetEnumerator()
			=> _mappings.Keys.GetEnumerator();
		
		IEnumerator IEnumerable.GetEnumerator()
			=> _mappings.Keys.GetEnumerator();
	}
	
	public class TextureCell
	{
		public Vector2 TopLeft { get; }
		public Vector2 TopRight { get; }
		public Vector2 BottomLeft { get; }
		public Vector2 BottomRight { get; }
		
		public TextureCell(float x1, float y1, float x2, float y2)
		{
			TopLeft     = new Vector2(x1, y1);
			TopRight    = new Vector2(x2, y1);
			BottomLeft  = new Vector2(x1, y2);
			BottomRight = new Vector2(x2, y2);
		}
	}
}
