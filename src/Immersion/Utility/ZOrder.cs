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
		private const long COMPARE_MASK    = ~(~0L << 3) << (MAX_USABLE_BITS - 3);

		private static readonly ulong[] _masks = {
			0b_00000000_00000000_00000000_00000000_00000000_00011111_11111111_11111111, // 0x1fffff
			0b_00000000_00011111_00000000_00000000_00000000_00000000_11111111_11111111, // 0x1f00000000ffff
			0b_00000000_00011111_00000000_00000000_11111111_00000000_00000000_11111111, // 0x1f0000ff0000ff
			0b_00010000_00001111_00000000_11110000_00001111_00000000_11110000_00001111, // 0x100f00f00f00f00f
			0b_00010000_11000011_00001100_00110000_11000011_00001100_00110000_11000011, // 0x10c30c30c30c30c3
			0b_00010010_01001001_00100100_10010010_01001001_00100100_10010010_01001001, // 0x1249249249249249
		};

		private static readonly long MaskX = (long)_masks[_masks.Length - 1];
		private static readonly long MaskY = MaskX << 1;
		private static readonly long MaskZ = MaskX << 2;
		private static readonly long MaskXY = MaskX | MaskY;
		private static readonly long MaskXZ = MaskX | MaskZ;
		private static readonly long MaskYZ = MaskY | MaskZ;


		private readonly long _value;

		public int X => Decode(0);
		public int Y => Decode(1);
		public int Z => Decode(2);

		private ZOrder(long value)
			=> _value = ClearUnusedBits(value);

		public ZOrder(int x, int y, int z)
		{
			if (x < ELEMENT_MIN || x > ELEMENT_MAX) throw new ArgumentOutOfRangeException(nameof(x));
			if (y < ELEMENT_MIN || y > ELEMENT_MAX) throw new ArgumentOutOfRangeException(nameof(y));
			if (z < ELEMENT_MIN || z > ELEMENT_MAX) throw new ArgumentOutOfRangeException(nameof(z));
			_value = Split(x) | Split(y) << 1 | Split(z) << 2;
		}

		public void Deconstruct(out int x, out int y, out int z)
			=> (x, y, z) = (X, Y, Z);

		public ZOrder IncX() => new((((_value | MaskYZ) + 1 << 0) & MaskX) | (_value & MaskYZ));
		public ZOrder IncY() => new((((_value | MaskXZ) + 1 << 1) & MaskY) | (_value & MaskXZ));
		public ZOrder IncZ() => new((((_value | MaskXY) + 1 << 2) & MaskZ) | (_value & MaskXY));

		public ZOrder DecX() => new((((_value & MaskX) - 1 << 0) & MaskX) | (_value & MaskYZ));
		public ZOrder DecY() => new((((_value & MaskY) - 1 << 1) & MaskY) | (_value & MaskXZ));
		public ZOrder DecZ() => new((((_value & MaskZ) - 1 << 2) & MaskZ) | (_value & MaskXY));

		public static ZOrder operator +(ZOrder left, ZOrder right)
		{
			var xSum = (left._value | MaskYZ) + (right._value & MaskX);
			var ySum = (left._value | MaskXZ) + (right._value & MaskY);
			var zSum = (left._value | MaskXY) + (right._value & MaskZ);
			return new((xSum & MaskX) | (ySum & MaskY) | (zSum & MaskZ));
		}

		public static ZOrder operator -(ZOrder left, ZOrder right)
		{
			var xDiff = (left._value | MaskYZ) - (right._value & MaskX);
			var yDiff = (left._value | MaskXZ) - (right._value & MaskY);
			var zDiff = (left._value | MaskXY) - (right._value & MaskZ);
			return new((xDiff & MaskX) | (yDiff & MaskY) | (zDiff & MaskZ));
		}

		public static ZOrder operator &(ZOrder left, ZOrder right)
			=> new(left._value & right._value); // ClearUnusedBits unnecessary.
		public static ZOrder operator |(ZOrder left, ZOrder right)
			=> new(left._value | right._value); // ClearUnusedBits unnecessary.
		public static ZOrder operator ^(ZOrder left, ZOrder right)
			=> new(left._value ^ right._value); // ClearUnusedBits unnecessary.

		public static ZOrder operator <<(ZOrder left, int right)
		{
			if (right >= BITS_PER_ELEMENT) throw new ArgumentOutOfRangeException(
				nameof(right), right, $"{nameof(right)} must be smaller than {BITS_PER_ELEMENT}");
			return new(left._value << (right * 3));
		}
		public static ZOrder operator >>(ZOrder left, int right)
		{
			var result = left._value >> (right * 3);
			var mask   = (left._value >> (MAX_USABLE_BITS - 3)) << (MAX_USABLE_BITS - (right * 3));
			for (var i = 0; i < right; i++) { result |= mask; mask <<= 3; }
			return new(result);
		}

		public static explicit operator long(ZOrder order) => order._value;
		public static explicit operator ZOrder(long value) => new(value);

		public int CompareTo(ZOrder other) => (_value ^ COMPARE_MASK).CompareTo(other._value ^ COMPARE_MASK);
		public bool Equals(ZOrder other) => _value.Equals(other._value);
		public override bool Equals(object obj) => (obj is ZOrder order) && Equals(order);
		public override int GetHashCode() => _value.GetHashCode();
		public override string ToString() => $"ZOrder ({X}:{Y}:{Z})";


		private static long Split(int i)
		{
			var l = (ulong)i;
			// l = l & Masks[0];
			l = (l | l << 32) & _masks[1];
			l = (l | l << 16) & _masks[2];
			l = (l | l <<  8) & _masks[3];
			l = (l | l <<  4) & _masks[4];
			l = (l | l <<  2) & _masks[5];
			return (long)l;
		}

		private int Decode(int index)
		{
			var l = (ulong)_value >> index;
			l &= _masks[5];
			l = (l ^ (l >>  2)) & _masks[4];
			l = (l ^ (l >>  4)) & _masks[3];
			l = (l ^ (l >>  8)) & _masks[2];
			l = (l ^ (l >> 16)) & _masks[1];
			l = (l ^ (l >> 32)) & _masks[0];
			return ((int)l << SIGN_SHIFT) >> SIGN_SHIFT;
		}

		private static long ClearUnusedBits(long value)
			=> value & ~(~0L << MAX_USABLE_BITS);
	}
}
