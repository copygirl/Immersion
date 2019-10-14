using System;
using Godot;

namespace Immersion.Voxel
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
	
	public static class BlockFacingHelper
	{
		public static readonly BlockFacing[] ALL_FACINGS = {
			BlockFacing.East , BlockFacing.West ,
			BlockFacing.Up   , BlockFacing.Down ,
			BlockFacing.South, BlockFacing.North,
		};
		
		public static bool IsValid(this BlockFacing self)
			=> (self >= BlockFacing.East) && (self <= BlockFacing.North);
		
		public static Vector3 ToVector3(this BlockFacing self)
		{
			switch (self) {
				case BlockFacing.East  : return Vector3.Right;
				case BlockFacing.West  : return Vector3.Left;
				case BlockFacing.Up    : return Vector3.Up;
				case BlockFacing.Down  : return Vector3.Down;
				case BlockFacing.South : return Vector3.Forward;
				case BlockFacing.North : return Vector3.Back;
				default: throw new ArgumentException(
					$"'{self}' is not a valid BlockFacing", nameof(self));
			}
		}
		
		public static Vector3.Axis GetAxis(this BlockFacing self)
		{
			switch (self) {
				case BlockFacing.East  : case BlockFacing.West  : return Vector3.Axis.X;
				case BlockFacing.Up    : case BlockFacing.Down  : return Vector3.Axis.Y;
				case BlockFacing.South : case BlockFacing.North : return Vector3.Axis.Z;
				default: throw new ArgumentException(
					$"'{self}' is not a valid BlockFacing", nameof(self));
			}
		}
	}
}
