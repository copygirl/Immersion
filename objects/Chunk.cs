using System.Collections.Generic;
using Godot;
using Immersion.Voxel;
using Immersion.Voxel.Blocks;
using Immersion.Voxel.Chunks;

public class Chunk : Spatial
{
	public World World { get; }
	public ChunkPos ChunkPos { get; }
	public ChunkNeighbors Neighbors { get; }
	public IVoxelStorage<IBlock> Storage { get; } = new ChunkPaletteStorage<IBlock>(Block.AIR);
	public ICollection<string> AppliedGenerators { get; } = new HashSet<string>();

	// TODO: Replace this with something better.
	internal int NumGeneratedNeighbors = -1;

	public Chunk(World world, ChunkPos pos)
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
