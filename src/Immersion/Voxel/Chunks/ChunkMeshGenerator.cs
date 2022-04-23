using Godot;
using Immersion.Voxel.Blocks;

namespace Immersion.Voxel.Chunks
{
	public class ChunkMeshGenerator
	{
		private readonly World _world;
		private readonly SurfaceTool _surface = new();
		private readonly IBlock[] _neighbors = new IBlock[BlockFacings.ALL.Count];

		public ChunkMeshGenerator(World world) => _world = world;

		public ArrayMesh? Generate(IChunk chunk)
		{
			_surface.Begin(Mesh.PrimitiveType.Triangles);
			_surface.SetMaterial(_world.TerrainMaterial);

			var center = chunk.Neighbors[0, 0, 0]!;
			for (var x = 0; x < 16; x++)
			for (var y = 0; y < 16; y++)
			for (var z = 0; z < 16; z++) {
				var block = center.Storage[x, y, z];
				foreach (var facing in BlockFacings.ALL)
					_neighbors[(int)facing] = GetNeighborBlock(chunk.Neighbors, x, y, z, facing);
				block.Model.RenderIntoMesh(block, (x, y, z), _world.TerrainAtlas, _surface, _neighbors);
			}

			var mesh = _surface.Commit();
			return (mesh.GetSurfaceCount() > 0) ? mesh : null;
		}

		private static IBlock GetNeighborBlock(ChunkNeighbors chunks,
			int x, int y, int z, BlockFacing facing)
		{
			var cx = 0; var cy = 0; var cz = 0;
			switch (facing) {
				case BlockFacing.East  : x += 1; if (x >= 16) cx += 1; break;
				case BlockFacing.West  : x -= 1; if (x <   0) cx -= 1; break;
				case BlockFacing.Up    : y += 1; if (y >= 16) cy += 1; break;
				case BlockFacing.Down  : y -= 1; if (y <   0) cy -= 1; break;
				case BlockFacing.South : z += 1; if (z >= 16) cz += 1; break;
				case BlockFacing.North : z -= 1; if (z <   0) cz -= 1; break;
			}
			return chunks[cx, cy, cz]?.Storage[x & 0b1111, y & 0b1111, z & 0b1111] ?? Block.AIR;
		}
	}
}
