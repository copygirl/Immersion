using System;
using Godot;

namespace Immersion.Voxel
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
		
		private static readonly int[] TRIANGLE_INDICES = { 0, 3, 1,  1, 3, 2 };
		
		public ArrayMesh Generate(ChunkVoxelStorage chunk,
		                          Material material, TextureAtlas<byte> atlas)
		{
			if (chunk == null) throw new ArgumentNullException(nameof(chunk));
			
			var st = new SurfaceTool();
			st.Begin(Mesh.PrimitiveType.Triangles);
			st.SetMaterial(material);
			
			for (var x = 0; x < 16; x++)
			for (var y = 0; y < 16; y++)
			for (var z = 0; z < 16; z++) {
				var block = chunk[x, y, z];
				// TODO: Replace with IBlock.IsAir
				if (block == 0) continue;
				
				var textureCell = atlas[block];
				var blockVertex = new Vector3(x, y, z);
				foreach (var facing in BlockFacingHelper.ALL_FACINGS) {
					var neighbor = GetNeighborBlock(chunk, x, y, z, facing);
					// TODO: Replace with IBlock.IsSideCulled
					if (neighbor != 0) continue;
					
					var vertIndex = (int)facing << 2;
					var normal = facing.ToVector3();
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
						st.AddNormal(normal);
						// st.AddColor(COLORS_PER_FACING[(int)facing]);
						st.AddVertex(blockVertex + VERTICES_PER_FACING[vertIndex | j]);
					}
				}
			}
			
			return st.Commit();
		}
		
		private const int VALID_INDEX = ~0b1111;
		private byte GetNeighborBlock(ChunkVoxelStorage chunk,
		                              int x, int y, int z, BlockFacing facing)
		{
			switch (facing) {
				case BlockFacing.East  : x += 1; break;
				case BlockFacing.West  : x -= 1; break;
				case BlockFacing.Up    : y += 1; break;
				case BlockFacing.Down  : y -= 1; break;
				case BlockFacing.South : z += 1; break;
				case BlockFacing.North : z -= 1; break;
			}
			return ((x & VALID_INDEX) == 0) && ((y & VALID_INDEX) == 0) && ((z & VALID_INDEX) == 0)
				? chunk[x, y, z] : (byte)0;
		}
	}
}
