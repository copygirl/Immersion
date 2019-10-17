using Immersion.Voxel.Chunks;

namespace Immersion.Voxel.WorldGen
{
	public interface IWorldGenerator
	{
		void Populate(IChunk chunk);
	}
}
