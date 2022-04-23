using System.Collections.Generic;
using Godot;
using Immersion.Voxel;
using Immersion.Voxel.Blocks;
using Immersion.Voxel.Chunks;

public interface IChunk
{
	IWorld World { get; }
	ChunkPos ChunkPos { get; }
	ChunkNeighbors Neighbors { get; }
	IVoxelStorage<IBlock> Storage { get; }
	ICollection<string> AppliedGenerators { get; }
}

public class Chunk
	: Spatial
	, IChunk
{
	public IWorld World { get; }
	public ChunkPos ChunkPos { get; }
	public ChunkNeighbors Neighbors { get; }
	public IVoxelStorage<IBlock> Storage { get; } = new ChunkPaletteStorage<IBlock>(Block.AIR);
	public ICollection<string> AppliedGenerators { get; } = new HashSet<string>();

	// TODO: Replace this with something better.
	internal int NumGeneratedNeighbors = -1;

	public Chunk(IWorld world, ChunkPos pos)
	{
		Name      = $"Chunk {pos}";
		World     = world;
		ChunkPos  = pos;
		Transform = new(Basis.Identity, pos.GetOrigin());
		Neighbors = new(this);
	}

	public override void _Ready()
	{
		AddChild(new Trackable {
			TrackAutomatically = false,
			Translation = new(8.0F, 8.0F, 8.0F),
			Shape = new BoxShape { Extents = new(8.5F, 8.5F, 8.5F) },
		});
	}
}
