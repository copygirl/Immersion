using Immersion.Utility;

namespace Immersion.Tests
{
	public static class Test_ZOrder
	{
		[Test]
		public static void Decoding()
		{
			var value = new ZOrder(6, -16, 15);
			Assert.Equal(  6, value.X);
			Assert.Equal(-16, value.Y);
			Assert.Equal( 15, value.Z);

			var (x, y, z) = value;
			Assert.Equal((6, -16, 15), (x, y, z));
		}

		[Test]
		public static void Raw()
		{
			const long RAW_VALUE = 0b_010_010_010_010_010_010_010_010_010_010_010_010_010_010_010_010_010_100_101_101_100;
			var value = new ZOrder(6, -16, 15);
			Assert.Equal(RAW_VALUE, value.Raw);
			Assert.Equal(value, ZOrder.FromRaw(RAW_VALUE));
		}

		[Test]
		public static void BitShifting()
		{
			var zero = new ZOrder(0, 0, 0);
			Assert.Equal(zero, zero >> 2);
			Assert.Equal(zero, zero << 2);

			var pos123 = new ZOrder(1, 2, 3);
			Assert.Equal(new(4, 8, 12), pos123 << 2);
			Assert.Equal(pos123, new ZOrder(4, 8, 12) >> 2);

			var neg123 = new ZOrder(-1, -2, -3);
			Assert.Equal(new(-4, -8, -12), neg123 << 2);
			Assert.Equal(neg123, new ZOrder(-4, -8, -12) >> 2);
		}

		[Test]
		public static void IncAndDec()
		{
			var value = new ZOrder(5, 0, -200);

			Assert.Equal(new(6, 0, -200), value.IncX());
			Assert.Equal(new(5, 1, -200), value.IncY());
			Assert.Equal(new(5, 0, -199), value.IncZ());

			Assert.Equal(new(4,  0, -200), value.DecX());
			Assert.Equal(new(5, -1, -200), value.DecY());
			Assert.Equal(new(5,  0, -201), value.DecZ());
		}

		[Test]
		public static void AddAndSubtract()
		{
			var value = new ZOrder(1, 2, 3);
			var diff  = new ZOrder(10, -20, 0);

			Assert.Equal(new(11, -18, 3), value + diff);
			Assert.Equal(new(-9,  22, 3), value - diff);
		}

		[Test]
		public static void AddAndSubtract_ForLoops()
		{
			for (var x = -1; x <= 1; x++)
			for (var y = -1; y <= 1; y++)
			for (var z = -1; z <= 1; z++) {
				Assert.Equal(new ZOrder( x,  y,  z), new ZOrder(0, 0, 0) + new ZOrder(x, y, z));
				Assert.Equal(new ZOrder(-x, -y, -z), new ZOrder(0, 0, 0) - new ZOrder(x, y, z));
			}
		}
	}
}
