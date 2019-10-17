using Immersion.Voxel.Chunks;

namespace Immersion.Voxel.WorldGen
{
	public interface IWorldGenerator
	{
		string Identifier { get; }
		
		int Priority { get; }
		
		bool Populate(IChunk chunk);
	}
}
