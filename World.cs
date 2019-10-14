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
		private readonly Dictionary<ChunkPos, MeshInstance> _chunks =
			new Dictionary<ChunkPos, MeshInstance>();
		
		private Material _material;
		private TextureAtlas<byte> _atlas;
		private ChunkMeshGenerator _generator;
		
		[Export]
		public Texture Texture { get; set; }
		[Export]
		public int TextureCellSize { get; set; } = 16;
		
		public override void _Ready()
		{
			_material = new SpatialMaterial {
				AlbedoTexture = Texture,
				VertexColorUseAsAlbedo = true,
			};
			
			_atlas = new TextureAtlas<byte>(Texture.GetWidth(), Texture.GetHeight(), TextureCellSize){
				{ (byte)0, 0, 0 },
				{ (byte)1, 1, 0 },
				{ (byte)2, 2, 0 },
				{ (byte)3, 3, 0 },
			};
			
			_generator = new ChunkMeshGenerator();
			
			for (int x = -4; x < 4; x++)
			for (int y =  0; y < 4; y++)
			for (int z = -4; z < 4; z++)
				GenerateChunk(x, y, z);
		}
		
		public void GenerateChunk(int x, int y, int z)
		{
			var chunk = new ChunkVoxelStorage();
			for (var cx = 0; cx < 16; cx++)
			for (var cy = 0; cy < 16; cy++)
			for (var cz = 0; cz < 16; cz++) {
				var yy = y * 16 + cy;
				if (_rnd.Next(yy / 2 + 1) != 0) continue;
				chunk[cx, cy, cz] = (yy <  8) ? (byte)1
				                  : (yy < 48) ? (byte)2
				                              : (byte)3;
			}
			
			var mesh  = _generator.Generate(chunk, _material, _atlas);
			var shape = mesh.CreateTrimeshShape();
			// TODO: Create collision mesh from box colliders? Should be cheaper.
			
			var chunkPos  = new ChunkPos(x, y, z);
			var transform = new Transform(Basis.Identity,
				new Vector3(x * 16, y * 16, z * 16));
			var meshInstance = new MeshInstance {
				Name      = $"Chunk {chunkPos}",
				Mesh      = mesh,
				Transform = transform,
			};
			
			var body = new StaticBody();
			body.AddChild(new CollisionShape { Shape = shape });
			meshInstance.AddChild(body);
			
			_chunks.Add(chunkPos, meshInstance);
			AddChild(meshInstance);
		}
	}
	
	public struct ChunkPos : IEquatable<ChunkPos>
	{
		public int X { get; }
		public int Y { get; }
		public int Z { get; }
		
		public ChunkPos(int x, int y, int z)
		{
			this.X = x;
			this.Y = y;
			this.Z = z;
		}
		
		public bool Equals(ChunkPos other)
			=> (X == other.X) && (Y == other.Y) && (Z == other.Z);
		
		public override bool Equals(object obj)
			=> (obj is ChunkPos) && Equals((ChunkPos)obj);
		
		public override int GetHashCode()
		{
			unchecked {
				int hash = 17;
				hash = hash * 23 + X;
				hash = hash * 23 + Y;
				hash = hash * 23 + Z;
				return hash;
			}
		}
		
		public override string ToString()
			=> $"[{X}:{Y}:{Z}]";
	}
}
