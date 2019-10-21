
namespace Immersion.Voxel.Blocks
{
	public interface IBlock
	{
		bool IsAir { get; }
		
		string Texture { get; }
		
		IBlockModel Model { get; }
	}
}
