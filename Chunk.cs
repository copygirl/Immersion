using Godot;
using Immersion.Voxel;

namespace Immersion
{
	public class Chunk : Spatial
	{
		public World World { get; }
		public ChunkPos ChunkPos { get; }
		public Chunk[,,] ChunkNeighbors { get; } = new Chunk[3, 3, 3];
		
		public ChunkPaletteStorage<IBlock> ChunkStorage { get; } =
			new ChunkPaletteStorage<IBlock>(Block.AIR);
		
		public Chunk(World world, ChunkPos pos)
		{
			World     = world;
			ChunkPos  = pos;
			Transform = new Transform(Basis.Identity, pos.GetOrigin());
			ChunkNeighbors[1, 1, 1] = this;
		}
		
		public void GenerateBlocks(OpenSimplexNoise noise)
		{
			// cx, cy, cz = Position of the chunk in the chunk grid.
			// lx, ly, lz = Local position of the block in the chunk.
			// gx, gy, gz = Global position of the blcok in the world.
			for (var lx = 0; lx < 16; lx++)
			for (var ly = 0; ly < 16; ly++)
			for (var lz = 0; lz < 16; lz++) {
				var gx = ChunkPos.X << 4 | lx;
				var gy = ChunkPos.Y << 4 | ly;
				var gz = ChunkPos.Z << 4 | lz;
				var bias = Mathf.Clamp((gy / 48.0F - 0.5F), -0.5F, 1.0F);
				if (noise.GetNoise3d(gx, gy, gz) > bias)
					ChunkStorage[lx, ly, lz] = Block.STONE;
			}
		}
		
		public void GenerateMesh(ChunkMeshGenerator generator)
		{
			var chunks = new IVoxelView<IBlock>?[3, 3, 3];
			for (var x = 0; x < 3; x++)
			for (var y = 0; y < 3; y++)
			for (var z = 0; z < 3; z++)
				chunks[x, y, z] = ChunkNeighbors[x, y, z]?.ChunkStorage;
			
			var mesh  = generator.Generate(chunks);
			var shape = mesh.CreateTrimeshShape();
			
			var meshInstance = new MeshInstance { Mesh = mesh };
			var staticBody   = new StaticBody();
			staticBody.AddChild(new CollisionShape { Shape = shape });
			
			AddChild(meshInstance);
			AddChild(staticBody);
		}
	}
}
