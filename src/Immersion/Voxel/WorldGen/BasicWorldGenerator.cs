using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Immersion.Voxel.Blocks;

namespace Immersion.Voxel.WorldGen
{
	public class BasicWorldGenerator
		: IWorldGenerator
	{
		public static readonly string IDENTIFIER = nameof(BasicWorldGenerator);

		private readonly OpenSimplexNoise _noise = new(){
			Seed = new Random().Next(),
			Octaves     = 4,
			Persistence = 0.6F,
		};

		public string Identifier { get; } = IDENTIFIER;

		public IEnumerable<string> Dependencies
			=> Enumerable.Empty<string>();

		public IEnumerable<(Neighbor, string)> NeighborDependencies
			=> Enumerable.Empty<(Neighbor, string)>();

		public void Populate(IChunk chunk)
		{
			for (var lx = 0; lx < Chunk.LENGTH; lx++)
			for (var ly = 0; ly < Chunk.LENGTH; ly++)
			for (var lz = 0; lz < Chunk.LENGTH; lz++) {
				var gx = chunk.ChunkPos.X << Chunk.BIT_SHIFT | lx;
				var gy = chunk.ChunkPos.Y << Chunk.BIT_SHIFT | ly;
				var gz = chunk.ChunkPos.Z << Chunk.BIT_SHIFT | lz;
				var bias = Mathf.Clamp(gy / 64.0F - 0.5F, -0.25F, 1.0F);
				if (_noise.GetNoise3d(gx, gy, gz) > bias)
					chunk.Storage[lx, ly, lz] = Block.STONE;
			}
		}
	}
}
