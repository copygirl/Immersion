using System;
using Godot;
using Immersion.Voxel.Chunks;

namespace Immersion.Voxel.WorldGen
{
	public class BasicWorldGenerator
		: IWorldGenerator
	{
		private readonly OpenSimplexNoise _noise = new OpenSimplexNoise {
			Seed = new Random().Next(),
			Octaves     = 4,
			Persistence = 0.5F,
		};
		
		public int Seed {
			get => _noise.Seed;
			set => _noise.Seed = value;
		}
		
		public void Populate(IChunk chunk)
		{
			for (var lx = 0; lx < 16; lx++)
			for (var ly = 0; ly < 16; ly++)
			for (var lz = 0; lz < 16; lz++) {
				var gx = chunk.Position.X << 4 | lx;
				var gy = chunk.Position.Y << 4 | ly;
				var gz = chunk.Position.Z << 4 | lz;
				var bias = Mathf.Clamp((gy / 48.0F - 0.5F), -0.5F, 1.0F);
				if (_noise.GetNoise3d(gx, gy, gz) > bias)
					chunk.Storage[lx, ly, lz] = Block.STONE;
			}
		}
	}
}
