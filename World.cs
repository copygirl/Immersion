using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Immersion.Voxel;
using Immersion.Voxel.Chunk;


namespace Immersion
{
	[Tool]
	public class World : Spatial
	{
		private readonly Random _rnd = new Random();
		private readonly Dictionary<ChunkPos, Chunk> _chunks =
			new Dictionary<ChunkPos, Chunk>();
		
		private Spatial? _tracked;
		private ChunkMeshGenerator? _generator;
		private OpenSimplexNoise? _noise;
		
		[Export]
		public Texture? Texture { get; set; }
		[Export]
		public int TextureCellSize { get; set; } = 16;
		
		public override void _Ready()
		{
			_tracked = GetNode<Spatial>("../Player");
			
			var material = new SpatialMaterial {
				AlbedoTexture = Texture,
				VertexColorUseAsAlbedo = true,
			};
			
			var atlas = new TextureAtlas<string>(
				Texture!.GetWidth(), Texture.GetHeight(), TextureCellSize);
			atlas.Add("air"  , 0, 0);
			atlas.Add("stone", 1, 0);
			atlas.Add("dirt" , 2, 0);
			atlas.Add("grass", 3, 0);
			
			_generator = new ChunkMeshGenerator(this, material, atlas);
			
			_noise = new OpenSimplexNoise {
				Seed        = _rnd.Next(),
				Octaves     = 4,
				Persistence = 0.5F,
			};
			
			for (var x = -3; x < 3; x++)
			for (var y =  0; y < 4; y++)
			for (var z = -3; z < 3; z++)
				GenerateChunk(x, y, z);
			
			for (var x = -3; x < 3; x++)
			for (var z = -3; z < 3; z++)
				CoverChunksWithGrass(x, z);
			
			for (var x = -2; x < 2; x++)
			for (var y =  0; y < 4; y++)
			for (var z = -2; z < 2; z++)
				GenerateChunkMesh(x, y, z);
		}
		
		public override void _Process(float delta)
		{
			if (Engine.EditorHint) return;
			var pos = _tracked!.GlobalTransform.origin.ToChunkPos();
			GenerateNearbyChunks(pos);
			GenerateNearbyChunkMeshes(pos);
			RemoveFarAwayChunks(pos);
		}
		
		private void GenerateNearbyChunks(ChunkPos center, int distance = 12)
		{
			for (var x = 0; x < distance; x = (x >= 0) ? -(x + 1) : -x)
			for (var z = 0; z < distance; z = (z >= 0) ? -(z + 1) : -z)
			if (!_chunks.TryGetValue((center.X + x, 0, center.Z + z), out var chunk)) {
				for (var y = 0; y < 4; y++)
					GenerateChunk(center.X + x, y, center.Z + z);
				CoverChunksWithGrass(center.X + x, center.Z + z);
				return;
			}
		}
		
		private void GenerateNearbyChunkMeshes(ChunkPos center, int distance = 11)
		{
			bool HasNeighbors(Chunk chunk)
				=> (chunk.Neighbors[ 1, 0, 0]?.IsGenerated == true)
				&& (chunk.Neighbors[-1, 0, 0]?.IsGenerated == true)
				&& (chunk.Neighbors[ 0, 0, 1]?.IsGenerated == true)
				&& (chunk.Neighbors[ 0, 0,-1]?.IsGenerated == true);
			
			for (var x = 0; x < distance; x = (x >= 0) ? -(x + 1) : -x)
			for (var z = 0; z < distance; z = (z >= 0) ? -(z + 1) : -z)
			if (_chunks.TryGetValue((center.X + x, 0, center.Z + z), out var chunk)
			 && !chunk.HasMesh && HasNeighbors(chunk)) {
				for (var y = 0; y < 4; y++)
					GenerateChunkMesh(center.X + x, y, center.Z + z);
				return;
			}
		}
		
		private void RemoveFarAwayChunks(ChunkPos center, float distance = 16)
		{
			var tooFarChunks = _chunks.Values
				.Where(chunk => (Math.Abs(chunk.Position.X - center.X) > distance)
				             || (Math.Abs(chunk.Position.Z - center.Z) > distance))
				.ToList();
			foreach (var chunk in tooFarChunks) {
				_chunks.Remove(chunk.Position);
				RemoveChild(chunk);
			}
		}
		
		
		public void GenerateChunk(int cx, int cy, int cz)
		{
			var chunkPos = new ChunkPos(cx, cy, cz);
			var chunk    = new Chunk(this, chunkPos);
			_chunks[chunkPos] = chunk;
			
			for (var x = -1; x <= 1; x++)
			for (var y = -1; y <= 1; y++)
			for (var z = -1; z <= 1; z++)
			if (((x != 0) || (y != 0) || (z != 0))
			 && _chunks.TryGetValue(chunkPos.Add(x, y, z), out var neighbor)) {
				chunk.Neighbors[x, y, z] = neighbor;
				neighbor.Neighbors[-x, -y, -z] = chunk;
			}
			
			chunk.GenerateBlocks(_noise!);
		}
		
		public void CoverChunksWithGrass(int cx, int cz)
		{
			for (var lx = 0; lx < 16; lx++)
			for (var lz = 0; lz < 16; lz++) {
				var depth = 4;
				IChunk? chunk = null;
				for (var gy = 63; gy >= 0; gy--) {
					if (chunk?.Position.Y != (gy >> 4))
						chunk = _chunks[new ChunkPos(cx, (gy >> 4), cz)];
					var block = chunk.Storage[lx, gy & 0b1111, lz];
					if (block != Block.AIR) {
						chunk.Storage[lx, gy & 0b1111, lz] =
							(depth-- == 4) ? Block.GRASS : Block.DIRT;
						if (depth <= 0) break;
					} else if (depth < 4) break;
				}
			}
		}
		
		public void GenerateChunkMesh(int cx, int cy, int cz)
		{
			var chunkPos = new ChunkPos(cx, cy, cz);
			var chunk    = _chunks[chunkPos];
			chunk.GenerateMesh(_generator!);
			AddChild(chunk);
		}
	}
}
