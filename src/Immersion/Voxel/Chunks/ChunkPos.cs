using System;
using Godot;
using Immersion.Utility;
using Immersion.Voxel.Blocks;

namespace Immersion.Voxel.Chunks
{
	public readonly struct ChunkPos : IEquatable<ChunkPos>
	{
		public static readonly ChunkPos ORIGIN = default;

		public int X { get; }
		public int Y { get; }
		public int Z { get; }

		public ChunkPos(int x, int y, int z)
			=> (X, Y, Z) = (x, y, z);

		public void Deconstruct(out int x, out int y, out int z)
			=> (x, y, z) = (X, Y, Z);


		public Vector3 GetOrigin() => new(
			X << Chunk.BIT_SHIFT, Y << Chunk.BIT_SHIFT, Z << Chunk.BIT_SHIFT);
		public Vector3 GetCenter() => new(
			(X << Chunk.BIT_SHIFT) + Chunk.LENGTH / 2,
			(Y << Chunk.BIT_SHIFT) + Chunk.LENGTH / 2,
			(Z << Chunk.BIT_SHIFT) + Chunk.LENGTH / 2);


		public ChunkPos Add(int x, int y, int z) => new(X + x, Y + y, Z + z);
		public ChunkPos Add(in ChunkPos other)   => new(X + other.X, Y + other.Y, Z + other.Z);

		public ChunkPos Add(BlockFacing facing)
			{ var (x, y, z) = facing; return Add(x, y, z); }
		public ChunkPos Add(Neighbor neighbor)
			{ var (x, y, z) = neighbor; return Add(x, y, z); }

		public ChunkPos Subtract(int x, int y, int z) => new(X - x, Y - y, Z - z);
		public ChunkPos Subtract(in ChunkPos other)   => new(X - other.X, Y - other.Y, Z - other.Z);

		public ChunkPos Subtract(BlockFacing facing)
			{ var (x, y, z) = facing; return Subtract(x, y, z); }
		public ChunkPos Subtract(Neighbor neighbor)
			{ var (x, y, z) = neighbor; return Subtract(x, y, z); }


		public bool Equals(ChunkPos other)
			=> (X == other.X) && (Y == other.Y) && (Z == other.Z);
		public override bool Equals(object obj)
			=> (obj is ChunkPos pos) && Equals(pos);

		public override int GetHashCode() => HashCode.Combine(X, Y, Z);
		public override string ToString() => $"<{X},{Y},{Z}>";


		public static implicit operator ChunkPos((int x, int y, int z) t) => new(t.x, t.y, t.z);
		public static implicit operator (int, int, int)(ChunkPos pos)     => (pos.X, pos.Y, pos.Z);

		public static ChunkPos operator +(ChunkPos left, ChunkPos right) => left.Add(right);
		public static ChunkPos operator -(ChunkPos left, ChunkPos right) => left.Subtract(right);
		public static ChunkPos operator +(ChunkPos left, BlockFacing right) => left.Add(right);
		public static ChunkPos operator -(ChunkPos left, BlockFacing right) => left.Subtract(right);
		public static ChunkPos operator +(ChunkPos left, Neighbor right) => left.Add(right);
		public static ChunkPos operator -(ChunkPos left, Neighbor right) => left.Subtract(right);

		public static bool operator ==(ChunkPos left, ChunkPos right) => left.Equals(right);
		public static bool operator !=(ChunkPos left, ChunkPos right) => !left.Equals(right);
	}

	public static class ChunkPosExtensions
	{
		public static ChunkPos ToChunkPos(this Vector3 pos) => new(
			Mathf.FloorToInt(pos.x) >> Chunk.BIT_SHIFT,
			Mathf.FloorToInt(pos.y) >> Chunk.BIT_SHIFT,
			Mathf.FloorToInt(pos.z) >> Chunk.BIT_SHIFT);

		public static ChunkPos ToChunkPos(this BlockPos self) => new(
			self.X >> Chunk.BIT_SHIFT, self.Y >> Chunk.BIT_SHIFT, self.Z >> Chunk.BIT_SHIFT);
		public static BlockPos ToChunkRelative(this BlockPos self) => new(
			self.X & Chunk.BIT_MASK, self.Y & Chunk.BIT_MASK, self.Z & Chunk.BIT_MASK);
		public static BlockPos ToChunkRelative(this BlockPos self, ChunkPos chunk) => new(
			self.X - (chunk.X << Chunk.BIT_SHIFT),
			self.Y - (chunk.Y << Chunk.BIT_SHIFT),
			self.Z - (chunk.Z << Chunk.BIT_SHIFT));
	}
}
