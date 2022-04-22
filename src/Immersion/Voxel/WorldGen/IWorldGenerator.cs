using System.Collections.Generic;

namespace Immersion.Voxel.WorldGen
{
	public interface IWorldGenerator
	{
		string Identifier { get; }

		IEnumerable<string> Dependencies { get; }

		IEnumerable<(Neighbor Neighbor, string Generator)> NeighborDependencies { get; }

		void Populate(Chunk chunk);
	}
}
