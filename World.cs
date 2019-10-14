using System;
using System.Collections.Generic;
using Godot;
using Immersion.Voxel;
using GDArray = Godot.Collections.Array;
using GDDict  = Godot.Collections.Dictionary;

namespace Immersion
{
#if TOOLS
	[Tool]
#endif
	public class World : Spatial
	{
		private readonly Random _rnd = new Random();
		private readonly Dictionary<ChunkPos, Chunk> _chunks =
			new Dictionary<ChunkPos, Chunk>();
		
		private ChunkMeshGenerator _generator;
		private OpenSimplexNoise _noise;
		
		[Export]
		public Texture Texture { get; set; }
		[Export]
		public int TextureCellSize { get; set; } = 16;
		
		public override void _Ready()
		{
			var material = new SpatialMaterial {
				AlbedoTexture = Texture,
				VertexColorUseAsAlbedo = true,
			};
			
			var atlas = new TextureAtlas<byte>(
				Texture.GetWidth(), Texture.GetHeight(), TextureCellSize);
			atlas.Add(0, 0, 0);
			atlas.Add(1, 1, 0);
			atlas.Add(2, 2, 0);
			atlas.Add(3, 3, 0);
			
			_generator = new ChunkMeshGenerator {
				Material = material,
				TextureAtlas = atlas,
			};
			
			_noise = new OpenSimplexNoise {
				Seed        = _rnd.Next(),
				Octaves     = 4,
				Persistence = 0.5F,
			};
			
			for (var x = -6; x < 6; x++)
			for (var y =  0; y < 4; y++)
			for (var z = -6; z < 6; z++)
				GenerateChunks(x, y, z);
			
			for (var x = -6; x < 6; x++)
			for (var z = -6; z < 6; z++)
				CoverChunksWithGrass(x, z);
			
			for (var x = -5; x < 5; x++)
			for (var y =  0; y < 4; y++)
			for (var z = -5; z < 5; z++)
				GenerateChunkMeshes(x, y, z);
		}
		
		public void GenerateChunks(int cx, int cy, int cz)
		{
			var chunkPos = new ChunkPos(cx, cy, cz);
			var chunk    = new Chunk(chunkPos);
			_chunks[chunkPos] = chunk;
			
			for (var x = -1; x <= 1; x++)
			for (var y = -1; y <= 1; y++)
			for (var z = -1; z <= 1; z++)
			if (((x != 0) || (y != 0) || (z != 0))
			 && _chunks.TryGetValue(chunkPos.Add(x, y, z), out var neighbor)) {
				chunk.ChunkNeighbors[1+x, 1+y, 1+z] = neighbor;
				neighbor.ChunkNeighbors[1-x, 1-y, 1-z] = chunk;
			}
			
			chunk.GenerateBlocks(_noise);
		}
		
		public void CoverChunksWithGrass(int cx, int cz)
		{
			for (var lx = 0; lx < 16; lx++)
			for (var lz = 0; lz < 16; lz++) {
				var depth = 4;
				Chunk chunk = null;
				for (var gy = 63; gy >= 0; gy--) {
					if (chunk?.ChunkPos.Y != (gy >> 4))
						chunk = _chunks[new ChunkPos(cx, (gy >> 4), cz)];
					ref var block = ref chunk.ChunkStorage[lx, gy & 0b1111, lz];
					if (block != 0) {
						block = (depth-- == 4) ? (byte)3 : (byte)2;
						if (depth <= 0) break;
					} else if (depth < 4) break;
				}
			}
		}
		
		public void GenerateChunkMeshes(int cx, int cy, int cz)
		{
			var chunkPos = new ChunkPos(cx, cy, cz);
			var chunk    = _chunks[chunkPos];
			chunk.GenerateMesh(_generator);
			AddChild(chunk);
		}
	}
}
