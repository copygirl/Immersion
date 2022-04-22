using System;

namespace Immersion.Utility
{
	// This struct wraps a primitive integer which represents an index into a space-filling curve
	// called "Z-Order Curve" (https://en.wikipedia.org/wiki/Z-order_curve). Often, this is also
	// referred to as Morton order, code, or encoding.
	//
	// This implementation purely focuses on 3 dimensions.
	//
	// By interleaving the 3 sub-elements into a single integer, some amount of packing can be
	// achieved, at the loss of some bits per elements. For example, with a 64 bit integer, 21
	// bits per elements are available (2_097_152 distinct values), which may be enough to
	// represent block coordinates in a bloxel game world.
	//
	// One upside of encoding separate coordinates into a single Z-Order index is that it can then
	// be effectively used to index into octrees, and certain operations such as bitwise shifting
	// are quite useful.
	public readonly struct ZOrder
		: IEquatable<ZOrder>
		, IComparable<ZOrder>
	{
		public const int ELEMENT_MIN = ~0 << (BITS_PER_ELEMENT - 1);
		public const int ELEMENT_MAX = ~ELEMENT_MIN;

		private const int BITS_SIZE        = sizeof(long) * 8;
		private const int BITS_PER_ELEMENT = BITS_SIZE / 3;
		private const int MAX_USABLE_BITS  = BITS_PER_ELEMENT * 3;
		private const int SIGN_SHIFT       = sizeof(int) * 8 - BITS_PER_ELEMENT;
		private const long USABLE_MASK     = ~(~0L << MAX_USABLE_BITS);
		private const long COMPARE_MASK    = ~(~0L << 3) << (MAX_USABLE_BITS - 3);

		private static readonly ulong[] MASKS = {
			0b_00000000_00000000_00000000_00000000_00000000_00011111_11111111_11111111, // 0x1fffff
			0b_00000000_00011111_00000000_00000000_00000000_00000000_11111111_11111111, // 0x1f00000000ffff
			0b_00000000_00011111_00000000_00000000_11111111_00000000_00000000_11111111, // 0x1f0000ff0000ff
			0b_00010000_00001111_00000000_11110000_00001111_00000000_11110000_00001111, // 0x100f00f00f00f00f
			0b_00010000_11000011_00001100_00110000_11000011_00001100_00110000_11000011, // 0x10c30c30c30c30c3
			0b_00010010_01001001_00100100_10010010_01001001_00100100_10010010_01001001, // 0x1249249249249249
		};

		private static readonly long X_MASK = (long)MASKS[MASKS.Length - 1];
		private static readonly long Y_MASK = X_MASK << 1;
		private static readonly long Z_MASK = X_MASK << 2;
		private static readonly long XY_MASK = X_MASK | Y_MASK;
		private static readonly long XZ_MASK = X_MASK | Z_MASK;
		private static readonly long YZ_MASK = Y_MASK | Z_MASK;


		public long Raw { get; }

		public int X => Decode(0);
		public int Y => Decode(1);
		public int Z => Decode(2);


		private ZOrder(long value)
			=> Raw = value;

		public static ZOrder FromRaw(long value)
			=> new(value & USABLE_MASK);

		public ZOrder(int x, int y, int z)
		{
			if (x < ELEMENT_MIN || x > ELEMENT_MAX) throw new ArgumentOutOfRangeException(nameof(x));
			if (y < ELEMENT_MIN || y > ELEMENT_MAX) throw new ArgumentOutOfRangeException(nameof(y));
			if (z < ELEMENT_MIN || z > ELEMENT_MAX) throw new ArgumentOutOfRangeException(nameof(z));
			Raw = Split(x) | Split(y) << 1 | Split(z) << 2;
		}

		public void Deconstruct(out int x, out int y, out int z)
			=> (x, y, z) = (X, Y, Z);


		public ZOrder IncX() => FromRaw((((Raw | YZ_MASK) +  1      ) & X_MASK) | (Raw & YZ_MASK));
		public ZOrder IncY() => FromRaw((((Raw | XZ_MASK) + (1 << 1)) & Y_MASK) | (Raw & XZ_MASK));
		public ZOrder IncZ() => FromRaw((((Raw | XY_MASK) + (1 << 2)) & Z_MASK) | (Raw & XY_MASK));

		public ZOrder DecX() => FromRaw((((Raw & X_MASK) -  1      ) & X_MASK) | (Raw & YZ_MASK));
		public ZOrder DecY() => FromRaw((((Raw & Y_MASK) - (1 << 1)) & Y_MASK) | (Raw & XZ_MASK));
		public ZOrder DecZ() => FromRaw((((Raw & Z_MASK) - (1 << 2)) & Z_MASK) | (Raw & XY_MASK));

		public static ZOrder operator +(ZOrder left, ZOrder right)
		{
			var xSum = (left.Raw | YZ_MASK) + (right.Raw & X_MASK);
			var ySum = (left.Raw | XZ_MASK) + (right.Raw & Y_MASK);
			var zSum = (left.Raw | XY_MASK) + (right.Raw & Z_MASK);
			return FromRaw((xSum & X_MASK) | (ySum & Y_MASK) | (zSum & Z_MASK));
		}

		public static ZOrder operator -(ZOrder left, ZOrder right)
		{
			var xDiff = (left.Raw & X_MASK) - (right.Raw & X_MASK);
			var yDiff = (left.Raw & Y_MASK) - (right.Raw & Y_MASK);
			var zDiff = (left.Raw & Z_MASK) - (right.Raw & Z_MASK);
			return FromRaw((xDiff & X_MASK) | (yDiff & Y_MASK) | (zDiff & Z_MASK));
		}

		public static ZOrder operator &(ZOrder left, long right) => FromRaw(left.Raw & right);
		public static ZOrder operator |(ZOrder left, long right) => FromRaw(left.Raw | right);
		public static ZOrder operator ^(ZOrder left, long right) => FromRaw(left.Raw ^ right);

		public static ZOrder operator &(ZOrder left, ZOrder right) => new(left.Raw & right.Raw);
		public static ZOrder operator |(ZOrder left, ZOrder right) => new(left.Raw | right.Raw);
		public static ZOrder operator ^(ZOrder left, ZOrder right) => new(left.Raw ^ right.Raw);

		public static ZOrder operator <<(ZOrder left, int right)
		{
			if (right >= BITS_PER_ELEMENT) throw new ArgumentOutOfRangeException(
				nameof(right), right, $"{nameof(right)} must be smaller than {BITS_PER_ELEMENT}");
			return FromRaw(left.Raw << (right * 3));
		}
		public static ZOrder operator >>(ZOrder left, int right)
		{
			var result = left.Raw >> (right * 3);
			var mask   = (left.Raw >> (MAX_USABLE_BITS - 3)) << (MAX_USABLE_BITS - (right * 3));
			for (var i = 0; i < right; i++) { result |= mask; mask <<= 3; }
			return FromRaw(result);
		}

		public int CompareTo(ZOrder other) => (Raw ^ COMPARE_MASK).CompareTo(other.Raw ^ COMPARE_MASK);
		public bool Equals(ZOrder other) => Raw.Equals(other.Raw);
		public override bool Equals(object obj) => (obj is ZOrder order) && Equals(order);
		public override int GetHashCode() => Raw.GetHashCode();
		public override string ToString() => $"<{X},{Y},{Z}>";


		private static long Split(int i)
		{
			var l = (ulong)i;
			// l = l & Masks[0];
			l = (l | l << 32) & MASKS[1];
			l = (l | l << 16) & MASKS[2];
			l = (l | l <<  8) & MASKS[3];
			l = (l | l <<  4) & MASKS[4];
			l = (l | l <<  2) & MASKS[5];
			return (long)l;
		}

		private int Decode(int index)
		{
			var l = (ulong)Raw >> index;
			l &= MASKS[5];
			l = (l ^ (l >>  2)) & MASKS[4];
			l = (l ^ (l >>  4)) & MASKS[3];
			l = (l ^ (l >>  8)) & MASKS[2];
			l = (l ^ (l >> 16)) & MASKS[1];
			l = (l ^ (l >> 32)) & MASKS[0];
			return ((int)l << SIGN_SHIFT) >> SIGN_SHIFT;
		}
	}
}
