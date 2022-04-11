using System;
using Godot;
using Immersion.Utility;

namespace Immersion.Voxel.Blocks
{
	public enum BlockFacing
	{
		East,  // +X
		West,  // -X
		Up,    // +Y
		Down,  // -Y
		South, // +Z
		North, // -Z
	}

	public static class BlockFacings
	{
		public static readonly ImmutableSet<BlockFacing> HORIZONTALS =
			new(BlockFacing.East , BlockFacing.West ,
			    BlockFacing.South, BlockFacing.North);

		public static readonly ImmutableSet<BlockFacing> VERTICALS =
			new(BlockFacing.Up, BlockFacing.Down);

		public static readonly ImmutableSet<BlockFacing> ALL =
			new(HORIZONTALS, VERTICALS);
	}

	public static class BlockFacingExtensions
	{
		public static void Deconstruct(this BlockFacing self, out int x, out int y, out int z)
			=> (x, y, z) = self switch {
				BlockFacing.East  => (+1,  0,  0),
				BlockFacing.West  => (-1,  0,  0),
				BlockFacing.Up    => ( 0, +1,  0),
				BlockFacing.Down  => ( 0, -1,  0),
				BlockFacing.South => ( 0,  0, +1),
				BlockFacing.North => ( 0,  0, -1),
				_ => throw new ArgumentException(
					$"'{self}' is not a valid BlockFacing", nameof(self))
			};

		public static bool IsValid(this BlockFacing self)
			=> (self >= BlockFacing.East) && (self <= BlockFacing.North);

		public static BlockFacing GetOpposite(this BlockFacing self)
			=> (BlockFacing)((int)self ^ 0b1);

		public static Vector3 ToVector3(this BlockFacing self)
			=> self switch {
				BlockFacing.East  => Vector3.Right,
				BlockFacing.West  => Vector3.Left,
				BlockFacing.Up    => Vector3.Up,
				BlockFacing.Down  => Vector3.Down,
				BlockFacing.South => Vector3.Forward,
				BlockFacing.North => Vector3.Back,
				_ => throw new ArgumentException(
					$"'{self}' is not a valid BlockFacing", nameof(self))
			};
	}
}
