using System;

namespace Immersion.Voxel
{
	public class ChunkVoxelStorage
	{
		private readonly byte[] _array = new byte[16 * 16 * 16];
		
		public int Width  { get; } = 16;
		public int Height { get; } = 16;
		public int Depth  { get; } = 16;
		
		public ref byte this[int x, int y, int z]
			=> ref _array[GetIndex(x, y, z)];
		
		private const int VALID_INDEX = ~0b1111;
		private static int GetIndex(int x, int y, int z)
		{
			if ((x & VALID_INDEX) != 0) throw new ArgumentOutOfRangeException(
				nameof(x), x, $"{nameof(x)} (={x}) is out of range (0,16]");
			if ((y & VALID_INDEX) != 0) throw new ArgumentOutOfRangeException(
				nameof(y), y, $"{nameof(y)} (={y}) is out of range (0,16]");
			if ((z & VALID_INDEX) != 0) throw new ArgumentOutOfRangeException(
				nameof(z), z, $"{nameof(z)} (={z}) is out of range (0,16]");
			return x | y << 4 | z << 8;
		}
	}
}
