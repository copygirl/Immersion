
using System;
using Godot;
using Immersion.Utility;

namespace Immersion.Voxel
{
	public readonly struct BlockPos : IEquatable<BlockPos>
	{
		public static readonly BlockPos ORIGIN = new BlockPos(0, 0, 0);
		
		
		public int X { get; }
		public int Y { get; }
		public int Z { get; }
		
		public BlockPos(int x, int y, int z)
			=> (X, Y, Z) = (x, y, z);
		
		public void Deconstruct(out int x, out int y, out int z)
			=> (x, y, z) = (X, Y, Z);
		
		
		public static BlockPos FromChunkPos(ChunkPos pos)
			=> new BlockPos(pos.X << 4, pos.Y << 4, pos.Z << 4);
		public ChunkPos ToChunkPos()
			=> new ChunkPos(X >> 4, Y >> 4, Z >> 4);
		public BlockPos ToChunkRelative()
			=> new BlockPos(X & 0b1111, Y & 0b1111, Z & 0b1111);
		public BlockPos ToChunkRelative(ChunkPos pos)
			=> new BlockPos(X - (pos.X << 4), Y - (pos.Y << 4), Z - (pos.Z << 4));
		
		public static BlockPos FromVector3(Vector3 pos)
			=> new BlockPos(Mathf.FloorToInt(pos.x),
			                Mathf.FloorToInt(pos.y),
			                Mathf.FloorToInt(pos.z));
		public Vector3 GetOrigin()
			=> new Vector3(X, Y, Z);
		public Vector3 GetCenter()
			=> new Vector3(X + 0.5F, Y + 0.5F, Z + 0.5F);
		
		
		public BlockPos Add(int x, int y, int z)
			=> new BlockPos(X + x, Y + y, Z + z);
		public BlockPos Add(in BlockPos other)
			=> new BlockPos(X + other.X, Y + other.Y, Z + other.Z);
		
		public BlockPos Add(BlockFacing facing)
		{
			var (x, y, z) = facing;
			return Add(x, y, z);
		}
		public BlockPos Add(BlockFacing facing, int factor)
		{
			var (x, y, z) = facing;
			return Add(x * factor, y * factor, z * factor);
		}
		
		public BlockPos Subtract(int x, int y, int z)
			=> new BlockPos(X - x, Y - y, Z - z);
		public BlockPos Subtract(in BlockPos other)
			=> new BlockPos(X - other.X, Y - other.Y, Z - other.Z);
		
		public BlockPos Subtract(BlockFacing facing)
		{
			var (x, y, z) = facing;
			return Subtract(x, y, z);
		}
		public BlockPos Subtract(BlockFacing facing, int factor)
		{
			var (x, y, z) = facing;
			return Subtract(x * factor, y * factor, z * factor);
		}
		
		
		public bool Equals(BlockPos other)
			=> (X == other.X) && (Y == other.Y) && (Z == other.Z);
		public override bool Equals(object obj)
			=> (obj is BlockPos) && Equals((BlockPos)obj);
		
		public override int GetHashCode()
			=> HashHelper.Combine(X, Y, Z);
		public override string ToString()
			=> $"BlockPos [{X}:{Y}:{Z}]";
		
		
		public static implicit operator BlockPos((int x, int y, int z) t)
			=> new BlockPos(t.x, t.y, t.z);
		public static implicit operator (int, int, int)(BlockPos pos)
			=> (pos.X, pos.Y, pos.Z);
		
		public static BlockPos operator +(BlockPos left, BlockPos right)
			=> left.Add(right);
		public static BlockPos operator -(BlockPos left, BlockPos right)
			=> left.Subtract(right);
		public static BlockPos operator +(BlockPos left, BlockFacing right)
			=> left.Add(right);
		public static BlockPos operator -(BlockPos left, BlockFacing right)
			=> left.Subtract(right);
		
		public static bool operator ==(BlockPos left, BlockPos right)
			=> left.Equals(right);
		public static bool operator !=(BlockPos left, BlockPos right)
			=> !left.Equals(right);
	}
}
