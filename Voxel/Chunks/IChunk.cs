using System;
using System.Collections.Generic;

namespace Immersion.Voxel.Chunks
{
	public interface IChunk
	{
		World World { get; }
		ChunkPos Position { get; }
		ChunkState State { get; set; }
		ChunkNeighbors Neighbors { get; }
		IVoxelStorage<IBlock> Storage { get; }
		ICollection<string> AppliedGenerators { get; }
	}
	
	public enum ChunkState
	{
		/// <summary> New chunk ready to be filled. </summary>
		New,
		/// <summary> Currently asynchronously generating chunk data. </summary>
		Generating,
		/// <summary> Finished generating, filled with chunk data. </summary>
		Generated,
		/// <summary> Currently asynchronously generating mesh and collision data.
		///           May also be in this state after being updated, having to re-bake. </summary>
		Baking,
		/// <summary> Fully ready to be used in the world. </summary>
		Ready
	}
	
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
			get { var (x, y, z) = neighbor; return this[x, y, z]; }
			set { var (x, y, z) = neighbor; this[x, y, z] = value; }
		}
		
		public void Clear()
		{
			for (var i = 0; i < _chunks.Length; i++)
			if (i != CENTER_INDEX) _chunks[i] = null;
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
