using System.Collections.Generic;
using Godot;
using Immersion.Voxel;
using Immersion.Voxel.Blocks;
using Immersion.Voxel.Chunks;

namespace Immersion
{
	public class Chunk
		: Spatial, IChunk
	{
		public World World { get; }
		public ChunkPos Position { get; }
		public ChunkNeighbors Neighbors { get; }
		public IVoxelStorage<IBlock> Storage { get; }
			= new ChunkPaletteStorage<IBlock>(Block.AIR);
		public ICollection<string> AppliedGenerators { get; }
			= new HashSet<string>();

		public Chunk(World world, ChunkPos pos)
		{
			World     = world;
			Position  = pos;
			Transform = new Transform(Basis.Identity, pos.GetOrigin());
			Neighbors = new ChunkNeighbors(this);
		}
	}
}
