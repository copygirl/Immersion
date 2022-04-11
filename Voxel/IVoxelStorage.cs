namespace Immersion.Voxel
{
	public interface IVoxelView<T>
	{
		int Width  { get; } // X
		int Height { get; } // Y
		int Depth  { get; } // Z

		T this[int x, int y, int z] { get; }
	}

	public interface IVoxelStorage<T>
		: IVoxelView<T>
	{
		new T this[int x, int y, int z] { get; set; }
	}
}
