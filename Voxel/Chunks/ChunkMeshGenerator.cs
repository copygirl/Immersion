using System;
using Godot;

namespace Immersion.Voxel.Chunks
{
	public class ChunkMeshGenerator
	{
		private static readonly Vector3[] VERTICES_PER_FACING = {
			// East  (+X)
			new Vector3(1, 1, 1),
			new Vector3(1, 0, 1),
			new Vector3(1, 0, 0),
			new Vector3(1, 1, 0),
			// West  (-X)
			new Vector3(0, 1, 0),
			new Vector3(0, 0, 0),
			new Vector3(0, 0, 1),
			new Vector3(0, 1, 1),
			// Up    (+Y)
			new Vector3(1, 1, 0),
			new Vector3(0, 1, 0),
			new Vector3(0, 1, 1),
			new Vector3(1, 1, 1),
			// Down  (-Y)
			new Vector3(1, 0, 1),
			new Vector3(0, 0, 1),
			new Vector3(0, 0, 0),
			new Vector3(1, 0, 0),
			// South (+Z)
			new Vector3(0, 1, 1),
			new Vector3(0, 0, 1),
			new Vector3(1, 0, 1),
			new Vector3(1, 1, 1),
			// North (-Z)
			new Vector3(1, 1, 0),
			new Vector3(1, 0, 0),
			new Vector3(0, 0, 0),
			new Vector3(0, 1, 0),
		};
		
		// private static readonly Color[] COLORS_PER_FACING = {
		// 	Colors.Red,       // East  (+X)
		// 	Colors.DarkRed,   // West  (-X)
		// 	Colors.Green,     // Up    (+Y)
		// 	Colors.DarkGreen, // Down  (-Y)
		// 	Colors.Blue,      // South (+Z)
		// 	Colors.DarkBlue,  // North (-Z)
		// };
		
		private static readonly int[] TRIANGLE_INDICES
			= { 0, 3, 1,  1, 3, 2 };
		
		public World World { get; }
		public Material Material { get; set; }
		public TextureAtlas<string> TextureAtlas { get; set; }
		
		public ChunkMeshGenerator(World world, Material material,
		                          TextureAtlas<string> atlas)
		{
			World        = world;
			Material     = material;
			TextureAtlas = atlas;
		}
		
		public ArrayMesh Generate(ChunkNeighbors chunks)
		{
			var st = new SurfaceTool();
			st.Begin(Mesh.PrimitiveType.Triangles);
			st.SetMaterial(Material);
			
			var chunk = chunks[0, 0, 0]!;
			for (var x = 0; x < 16; x++)
			for (var y = 0; y < 16; y++)
			for (var z = 0; z < 16; z++) {
				var block = chunk.Storage[x, y, z];
				if (block.IsAir) continue;
				
				var textureCell = TextureAtlas[block.Texture];
				var blockVertex = new Vector3(x, y, z);
				foreach (var facing in BlockFacings.ALL) {
					if (block.IsSideCulled(facing)) {
						var neighbor = GetNeighborBlock(chunks, x, y, z, facing);
						if (neighbor.IsSideCulled(facing.GetOpposite())) continue;
					}
					
					var vertIndex = (int)facing << 2;
					st.AddNormal(facing.ToVector3());
					for (var i = 0; i < 6; i++) {
						var j = TRIANGLE_INDICES[i];
						Vector2 uv;
						switch (j) {
							case 0: uv = textureCell.TopLeft;     break;
							case 1: uv = textureCell.BottomLeft;  break;
							case 2: uv = textureCell.BottomRight; break;
							case 3: uv = textureCell.TopRight;    break;
							default: throw new InvalidOperationException();
						}
						st.AddUv(uv);
						st.AddVertex(blockVertex + VERTICES_PER_FACING[vertIndex | j]);
					}
				}
			}
			
			return st.Commit();
		}
		
		private IBlock GetNeighborBlock(
			ChunkNeighbors chunks, int x, int y, int z, BlockFacing facing)
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
