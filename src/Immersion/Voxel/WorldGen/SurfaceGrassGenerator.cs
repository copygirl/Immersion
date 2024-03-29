using System.Collections.Generic;
using Immersion.Voxel.Blocks;

namespace Immersion.Voxel.WorldGen
{
	// FIXME: There is an issue with this generator where it doesn't generate grass and dirt properly.
	public class SurfaceGrassGenerator
		: IWorldGenerator
	{
		public static readonly string IDENTIFIER = nameof(SurfaceGrassGenerator);

		private const int AIR_BLOCKS_NEEDED   = 12;
		private const int DIRT_BLOCKS_BENEATH =  3;

		public string Identifier { get; } = IDENTIFIER;

		public IEnumerable<string> Dependencies { get; } = new []{
			BasicWorldGenerator.IDENTIFIER
		};

		public IEnumerable<(Neighbor, string)> NeighborDependencies { get; } = new []{
			(Neighbor.Up, BasicWorldGenerator.IDENTIFIER)
		};

		public void Populate(IChunk chunk)
		{
			var up = chunk.Neighbors[Neighbor.Up]!;
			for (var lx = 0; lx < Chunk.LENGTH; lx++)
			for (var lz = 0; lz < Chunk.LENGTH; lz++) {
				var numAirBlocks = 0;
				var blockIndex   = 0;
				for (var ly = Chunk.LENGTH + AIR_BLOCKS_NEEDED - 1; ly >= 0; ly--) {
					var block = (ly >= Chunk.LENGTH)
						? up.Storage[lx, ly - Chunk.LENGTH, lz]
					    : chunk.Storage[lx, ly, lz];
					if (block.IsAir) {
						numAirBlocks++;
						blockIndex = 0;
					} else if ((numAirBlocks >= AIR_BLOCKS_NEEDED) || (blockIndex > 0)) {
						if (ly < Chunk.LENGTH) {
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
		}
	}
}
