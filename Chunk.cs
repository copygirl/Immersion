using Godot;
using Immersion.Voxel;
using Immersion.Voxel.Chunk;

namespace Immersion
{
	public class Chunk : Spatial, IChunk
	{
		public World World { get; }
		public ChunkPos Position { get; }
		public ChunkNeighbors Neighbors { get; } =
			new ChunkNeighbors();
		public IVoxelStorage<IBlock> Storage { get; } =
			new ChunkPaletteStorage<IBlock>(Block.AIR);
		
		public bool IsGenerated { get; private set; }
		public bool HasMesh { get; private set; }
		
		public Chunk(World world, ChunkPos pos)
		{
			World     = world;
			Position  = pos;
			Transform = new Transform(Basis.Identity, pos.GetOrigin());
			Neighbors[0, 0, 0] = this;
		}
		
		public void GenerateBlocks(OpenSimplexNoise noise)
		{
			// cx, cy, cz = Position of the chunk in the chunk grid.
			// lx, ly, lz = Local position of the block in the chunk.
			// gx, gy, gz = Global position of the blcok in the world.
			for (var lx = 0; lx < 16; lx++)
			for (var ly = 0; ly < 16; ly++)
			for (var lz = 0; lz < 16; lz++) {
				var gx = Position.X << 4 | lx;
				var gy = Position.Y << 4 | ly;
				var gz = Position.Z << 4 | lz;
				var bias = Mathf.Clamp((gy / 48.0F - 0.5F), -0.5F, 1.0F);
				if (noise.GetNoise3d(gx, gy, gz) > bias)
					Storage[lx, ly, lz] = Block.STONE;
			}
			
			IsGenerated = true;
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
