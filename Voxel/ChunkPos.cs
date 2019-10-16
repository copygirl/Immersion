using System;
using Godot;
using Immersion.Utility;

namespace Immersion.Voxel
{
	public readonly struct ChunkPos : IEquatable<ChunkPos>
	{
		public static readonly ChunkPos ORIGIN = new ChunkPos(0, 0, 0);
		
		
		public int X { get; }
		public int Y { get; }
		public int Z { get; }
		
		public ChunkPos(int x, int y, int z)
			=> (X, Y, Z) = (x, y, z);
		
		public void Deconstruct(out int x, out int y, out int z)
			=> (x, y, z) = (X, Y, Z);
		
		
		public static ChunkPos FromVector3(Vector3 pos)
			=> new ChunkPos(Mathf.FloorToInt(pos.x) >> 4,
			                Mathf.FloorToInt(pos.y) >> 4,
			                Mathf.FloorToInt(pos.z) >> 4);
		public Vector3 GetOrigin()
			=> new Vector3(X << 4, Y << 4, Z << 4);
		public Vector3 GetCenter()
			=> new Vector3((X << 4) + 0.5F, (Y << 4) + 0.5F, (Z << 4) + 0.5F);
		
		
		public ChunkPos Add(int x, int y, int z)
			=> new ChunkPos(X + x, Y + y, Z + z);
		public ChunkPos Add(in ChunkPos other)
			=> new ChunkPos(X + other.X, Y + other.Y, Z + other.Z);
		
		public ChunkPos Subtract(int x, int y, int z)
			=> new ChunkPos(X - x, Y - y, Z - z);
		public ChunkPos Subtract(in ChunkPos other)
			=> new ChunkPos(X - other.X, Y - other.Y, Z - other.Z);
		
		
		public bool Equals(ChunkPos other)
			=> (X == other.X) && (Y == other.Y) && (Z == other.Z);
		public override bool Equals(object obj)
			=> (obj is ChunkPos) && Equals((ChunkPos)obj);
		
		public override int GetHashCode()
			=> HashHelper.Combine(X, Y, Z);
		public override string ToString()
			=> $"ChunkPos [{X}:{Y}:{Z}]";
		
		
		public static implicit operator ChunkPos((int x, int y, int z) t)
			=> new ChunkPos(t.x, t.y, t.z);
		public static implicit operator (int, int, int)(ChunkPos pos)
			=> (pos.X, pos.Y, pos.Z);
		
		public static ChunkPos operator +(ChunkPos left, ChunkPos right)
			=> left.Add(right);
		public static ChunkPos operator -(ChunkPos left, ChunkPos right)
			=> left.Subtract(right);
		
		public static bool operator ==(ChunkPos left, ChunkPos right)
			=> left.Equals(right);
		public static bool operator !=(ChunkPos left, ChunkPos right)
			=> !left.Equals(right);
	}
}
