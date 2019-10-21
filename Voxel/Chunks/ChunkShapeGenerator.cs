using System.Collections.Generic;
using Godot;
using Immersion.Voxel.Blocks;

namespace Immersion.Voxel.Chunks
{
	public class ChunkShapeGenerator
	{
		private readonly List<Vector3> _buffer
			= new List<Vector3>();
		private readonly IBlock[] _neighbors
			= new IBlock[BlockFacings.ALL.Count];
		
		public Shape? Generate(IChunk chunk)
		{
			var center = chunk.Neighbors[0, 0, 0]!;
			for (var x = 0; x < 16; x++)
			for (var y = 0; y < 16; y++)
			for (var z = 0; z < 16; z++) {
				var block = center.Storage[x, y, z];
				foreach (var facing in BlockFacings.ALL)
					_neighbors[(int)facing] = GetNeighborBlock(chunk.Neighbors, x, y, z, facing);
				block.Model.AddCollisionShape(block, (x, y, z), _buffer, _neighbors);
			}
			
			if (_buffer.Count == 0) return null;
			var shape = new ConcavePolygonShape();
			shape.SetFaces(_buffer.ToArray());
			_buffer.Clear();
			return shape;
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
