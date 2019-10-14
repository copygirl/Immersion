using System;
using System.Collections.Generic;
using Godot;
using Immersion.Voxel;

namespace Immersion
{
	public class World : Spatial
	{
		private readonly Dictionary<ChunkPos, MeshInstance> _chunks
			= new Dictionary<ChunkPos, MeshInstance>();
		
		public override void _Ready()
		{
			for (int x = -4; x < 4; x++)
			for (int y =  0; y < 4; y++)
			for (int z = -4; z < 4; z++)
				GenerateChunk(x, y, z);
		}
		
		public void GenerateChunk(int x, int y, int z)
		{
			var rnd = new RandomNumberGenerator();
			var chunk = new ChunkVoxelStorage();
			for (var cx = 0; cx < 16; cx++)
			for (var cy = 0; cy < 16; cy++)
			for (var cz = 0; cz < 16; cz++)
				chunk[cx, cy, cz] = (rnd.RandiRange(0, y * 16 + cy) == 0) ? (byte)1 : (byte)0;
			
			var mesh  = new ChunkMeshGenerator().Generate(chunk);
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
