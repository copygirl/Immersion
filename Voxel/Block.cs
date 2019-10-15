
namespace Immersion.Voxel
{
	public class Block : IBlock
	{
		public static readonly Block AIR   = new Block("Air"  ){ IsAir = true };
		public static readonly Block STONE = new Block("Stone");
		public static readonly Block DIRT  = new Block("Dirt" );
		public static readonly Block GRASS = new Block("Grass");
		
		public string Name { get; }
		public string Texture { get; }
		public bool IsAir { get; set; }
		
		public Block(string name)
		{
			Name    = name;
			Texture = name.ToLowerInvariant();
		}
		
		public bool IsSideCulled(BlockFacing facing) => !IsAir;
	}
}