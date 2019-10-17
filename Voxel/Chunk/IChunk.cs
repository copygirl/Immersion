using System;

namespace Immersion.Voxel.Chunk
{
	public interface IChunk
	{
		World World { get; }
		ChunkPos Position { get; }
		ChunkNeighbors Neighbors { get; }
		IVoxelStorage<IBlock> Storage { get; }
		
		bool IsGenerated { get; }
		bool HasMesh { get; }
	}
	
	public class ChunkNeighbors
	{
		private IChunk[] _chunks = new IChunk[3 * 3 * 3];
		
		public IChunk this[int x, int y, int z] {
			get => _chunks[GetIndex(x, y, z)];
			set => _chunks[GetIndex(x, y, z)] = value;
		}
		public IChunk this[Neighbor neighbor] {
			get { var (x, y, z) = neighbor; return this[x, y, z]; }
			set { var (x, y, z) = neighbor; this[x, y, z] = value; }
		}
		
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
