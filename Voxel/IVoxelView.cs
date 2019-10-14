using System;

namespace Immersion.Voxel
{
	public interface IVoxelView<T>
	{
		int Width { get; }
		int Height { get; }
		int Depth { get; }
		
		T this[int x, int y, int z] { get; }
	}
}
