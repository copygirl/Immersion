using System;
using System.Collections.Generic;
using Godot;
using Immersion.Voxel;
using GDArray = Godot.Collections.Array;
using GDDict  = Godot.Collections.Dictionary;

namespace Immersion
{
	public class Chunk : Spatial
	{
		public ChunkPos ChunkPos { get; }
		public Chunk[,,] ChunkNeighbors { get; } = new Chunk[3,3,3];
		public ChunkVoxelStorage ChunkStorage { get; } = new ChunkVoxelStorage();
		
		public Chunk(ChunkPos pos)
		{
			ChunkPos  = pos;
			Transform = new Transform(Basis.Identity, new Vector3(
				ChunkPos.X << 4, ChunkPos.Y << 4, ChunkPos.Z << 4));
			ChunkNeighbors[1,1,1] = this;
		}
		
		public override void _Ready()
		{
		}
		
		public void GenerateBlocks(OpenSimplexNoise noise)
		{
			// cx, cy, cz = Position of the chunk in the chunk grid.
			// lx, ly, lz = Local position of the block in the chunk.
			// gx, gy, gz = Global position of the blcok in the world.
			for (var lx = 0; lx < 16; lx++)
			for (var ly = 0; ly < 16; ly++)
			for (var lz = 0; lz < 16; lz++) {
				var gx = ChunkPos.X << 4 | lx;
				var gy = ChunkPos.Y << 4 | ly;
				var gz = ChunkPos.Z << 4 | lz;
				var bias = Mathf.Clamp((gy / 48.0F - 0.5F), -0.5F, 1.0F);
				if (noise.GetNoise3d(gx, gy, gz) > bias)
					ChunkStorage[lx, ly, lz] = (byte)1;
			}
		}
		
		public void GenerateMesh(ChunkMeshGenerator generator)
		{
			var chunks = new ChunkVoxelStorage[3,3,3];
			for (var x = 0; x < 3; x++)
			for (var y = 0; y < 3; y++)
			for (var z = 0; z < 3; z++)
				chunks[x, y, z] = ChunkNeighbors[x, y, z]?.ChunkStorage;
			
			var mesh  = generator.Generate(chunks);
			var shape = mesh.CreateTrimeshShape();
			
			var meshInstance = new MeshInstance { Mesh = mesh };
			var staticBody   = new StaticBody();
			staticBody.AddChild(new CollisionShape { Shape = shape });
			
			AddChild(meshInstance);
			AddChild(staticBody);
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
		
		public ChunkPos Add(int x, int y, int z)
			=> new ChunkPos(X + x, Y + y, Z + z);
		
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
