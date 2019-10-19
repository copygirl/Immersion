using System;

namespace Immersion.Voxel.Chunks
{
	public class ChunkNeighbors
	{
		private static readonly int CENTER_INDEX = GetIndex(0, 0, 0);
		
		private readonly IChunk?[] _chunks
			= new IChunk?[3 * 3 * 3];
		
		public IChunk? this[int x, int y, int z] {
			get => _chunks[GetIndex(x, y, z)];
			set => _chunks[GetIndex(x, y, z)] = value;
		}
		public IChunk? this[Neighbor neighbor] {
			get => _chunks[GetIndex(neighbor)];
			set => _chunks[GetIndex(neighbor)] = value;
		}
		
		public ChunkNeighbors(IChunk center)
			=> this[0, 0, 0] = center;
		
		internal void Clear()
		{
			for (var i = 0; i < _chunks.Length; i++)
			if (i != CENTER_INDEX) _chunks[i] = null;
		}
		
		private static int GetIndex(Neighbor neighbor)
			{ var (x, y, z) = neighbor; return GetIndex(x, y, z); }
		private static int GetIndex(int x, int y, int z)
		{
			if ((x < -1) || (x > 1)) throw new ArgumentOutOfRangeException(
				nameof(x), x, $"{nameof(x)} (={x}) must be within (-1, 1)");
			if ((y < -1) || (y > 1)) throw new ArgumentOutOfRangeException(
				nameof(y), y, $"{nameof(y)} (={y}) must be within (-1, 1)");
			if ((z < -1) || (z > 1)) throw new ArgumentOutOfRangeException(
				nameof(z), z, $"{nameof(z)} (={z}) must be within (-1, 1)");
			return x+1 + (y+1) * 3 + (z+1) * 9;
		}
	}
}
