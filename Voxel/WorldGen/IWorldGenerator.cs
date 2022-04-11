using System.Collections.Generic;
using Immersion.Voxel.Chunks;

namespace Immersion.Voxel.WorldGen
{
	public interface IWorldGenerator
	{
		string Identifier { get; }

		IEnumerable<string> Dependencies { get; }

		IEnumerable<(Neighbor, string)> NeighborDependencies { get; }

		void Populate(World world, IChunk chunk);
	}
}
