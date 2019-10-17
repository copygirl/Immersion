using Immersion.Voxel.Chunks;

namespace Immersion.Voxel.WorldGen
{
	public class SurfaceGrassGenerator
		: IWorldGenerator
	{
		private const int AIR_BLOCKS_NEEDED   = 12;
		private const int DIRT_BLOCKS_BENEATH =  3;
		
		public string Identifier => nameof(SurfaceGrassGenerator);
		public int Priority => -80;
		
		public bool Populate(IChunk chunk)
		{
			// TODO: Above chunk might depend on this chunk being generated.
			//       Provide a collection of generators that have been used on a chunk.
			var up = chunk.Neighbors[Neighbor.Up];
			if (!(up?.State >= ChunkState.Generating)) return false;
			
			for (var lx = 0; lx < 16; lx++)
			for (var lz = 0; lz < 16; lz++) {
				var numAirBlocks = 0;
				var blockIndex   = 0;
				for (var ly = 15 + AIR_BLOCKS_NEEDED; ly >= 0; ly--) {
					var block = (ly >= 16) ? up.Storage[lx, ly - 16, lz]
										: chunk.Storage[lx, ly, lz];
					if (block.IsAir) {
						numAirBlocks++;
						blockIndex = 0;
					} else if ((numAirBlocks >= AIR_BLOCKS_NEEDED) || (blockIndex > 0)) {
						if (ly < 16) {
							if (blockIndex == 0)
								chunk.Storage[lx, ly, lz] = Block.GRASS;
							else if (blockIndex <= DIRT_BLOCKS_BENEATH)
								chunk.Storage[lx, ly, lz] = Block.DIRT;
						}
						blockIndex++;
						numAirBlocks = 0;
					}
				}
			}
			return true;
		}
	}
}
