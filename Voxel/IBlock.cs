using static Godot.Vector3;

namespace Immersion.Voxel
{
	public interface IBlock
	{
		bool IsAir { get; }
		bool IsSideCulled(BlockFacing facing);
	}
}
