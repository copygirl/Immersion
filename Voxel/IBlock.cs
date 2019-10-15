
namespace Immersion.Voxel
{
	public interface IBlock
	{
		bool IsAir { get; }
		
		bool IsSideCulled(BlockFacing facing);
		
		string Texture { get; }
	}
}
