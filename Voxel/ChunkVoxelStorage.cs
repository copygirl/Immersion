using System;

namespace Immersion.Voxel
{
	public class ChunkVoxelStorage
		: IVoxelView<byte>
	{
		private readonly byte[] _array = new byte[16 * 16 * 16];
		
		public int Width  { get; } = 16;
		public int Height { get; } = 16;
		public int Depth  { get; } = 16;
		
		public byte this[int x, int y, int z] {
			get => _array[GetIndex(x, y, z)];
			set => _array[GetIndex(x, y, z)] = value;
		}
		
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
