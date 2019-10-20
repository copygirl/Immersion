
namespace Immersion.Voxel.Blocks
{
	public interface IBlock
	{
		bool IsAir { get; }
		
		bool IsSideCulled(BlockFacing facing);
		
		string Texture { get; }
	}
}
