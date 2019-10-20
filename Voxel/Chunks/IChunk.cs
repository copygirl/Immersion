using System.Collections.Generic;
using Immersion.Voxel.Blocks;

namespace Immersion.Voxel.Chunks
{
	public interface IChunk
	{
		World World { get; }
		ChunkPos Position { get; }
		ChunkState State { get; }
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
		/// <summary> Finished loading / generating and ready to be baked. </summary>
		Prepared,
		/// <summary> Currently asynchronously generating mesh and collision data.
		///           May also be in this state after being updated, having to re-bake. </summary>
		Baking,
		/// <summary> Fully ready to be used in the world. </summary>
		Ready
	}
}
