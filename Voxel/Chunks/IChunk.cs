using System.Collections.Generic;
using Immersion.Voxel.Blocks;

namespace Immersion.Voxel.Chunks
{
	public interface IChunk
	{
		World World { get; }
		ChunkPos Position { get; }
		ChunkNeighbors Neighbors { get; }
		IVoxelStorage<IBlock> Storage { get; }
		ICollection<string> AppliedGenerators { get; }
	}
}
