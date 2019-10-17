using Godot;
using Immersion.Voxel;
using Immersion.Voxel.Chunks;

namespace Immersion
{
	public class Chunk
		: Spatial, IChunk
	{
		public World World { get; }
		public ChunkPos Position { get; }
		public ChunkNeighbors Neighbors { get; }
			= new ChunkNeighbors();
		public IVoxelStorage<IBlock> Storage { get; }
			= new ChunkPaletteStorage<IBlock>(Block.AIR);
		
		public bool IsGenerated { get; set; }
		public bool HasMesh { get; set; }
		
		public Chunk(World world, ChunkPos pos)
		{
			World     = world;
			Position  = pos;
			Transform = new Transform(Basis.Identity, pos.GetOrigin());
			Neighbors[0, 0, 0] = this;
		}
		
		public void GenerateMesh(ChunkMeshGenerator generator)
		{
			var mesh  = generator.Generate(Neighbors);
			var shape = mesh.CreateTrimeshShape();
			
			var meshInstance = new MeshInstance { Mesh = mesh };
			var staticBody   = new StaticBody();
			staticBody.AddChild(new CollisionShape { Shape = shape });
			
			AddChild(meshInstance);
			AddChild(staticBody);
			
			HasMesh = true;
		}
	}
}
