using Immersion.Utility;

namespace Immersion.Tests
{
	public static class Test_ZOrder
	{
		[Test]
		public static void Decoding()
		{
			var value = new ZOrder(6, -16, 15);
			Assert.Equal(value.X, 6);
			Assert.Equal(value.Y, -16);
			Assert.Equal(value.Z, 15);

			var (x, y, z) = value;
			Assert.Equal((x, y, z), (6, -16, 15));
		}

		[Test]
		public static void ToRawValue()
		{
			var value = new ZOrder(6, -16, 15);
			Assert.Equal((long)value, 0b_010_010_010_010_010_010_010_010_010_010_010_010_010_010_010_010_010_100_101_101_100);
		}

		[Test]
		public static void BitShifting()
		{
			var zero = new ZOrder(0, 0, 0);
			Assert.Equal(zero >> 2, zero);
			Assert.Equal(zero << 2, zero);

			var pos123 = new ZOrder(1, 2, 3);
			Assert.Equal(pos123 << 2, new ZOrder(4, 8, 12));
			Assert.Equal(new ZOrder(4, 8, 12) >> 2, pos123);

			var neg123 = new ZOrder(-1, -2, -3);
			Assert.Equal(neg123 << 2, new ZOrder(-4, -8, -12));
			Assert.Equal(new ZOrder(-4, -8, -12) >> 2, neg123);
		}
	}
}
