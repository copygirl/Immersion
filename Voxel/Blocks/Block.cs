
using System.Collections.Generic;
using Godot;

namespace Immersion.Voxel.Blocks
{
	public class Block : IBlock
	{
		public static readonly IBlock AIR   = new BlockAir();
		public static readonly IBlock STONE = new Block("Stone");
		public static readonly IBlock DIRT  = new Block("Dirt" );
		public static readonly IBlock GRASS = new Block("Grass");
		
		public string Name { get; }
		public string Texture { get; }
		public bool IsAir => false;
		public IBlockModel Model { get; set; }
		
		public Block(string name)
		{
			Name    = name;
			Texture = name.ToLowerInvariant();
			Model   = CubeBlockModel.INSTANCE;
		}
		
		public bool IsSideCulled(BlockFacing facing) => true;
	}
	
	public class BlockAir : IBlock
	{
		public string Texture => "air";
		public bool IsAir => true;
		public IBlockModel Model => new NullBlockModel();
		
		public bool IsSideCulled(BlockFacing facing) => false;
	}
	
	public class NullBlockModel : IBlockModel
	{
		public void RenderIntoMesh(IBlock block, BlockPos pos,
			TextureAtlas<string> textureAtlas,
			SurfaceTool st, IBlock[] neighborsByFacing) {  }
		
		public void AddCollisionShape(IBlock block, BlockPos pos,
			List<Vector3> triangles, IBlock[] neighborsByFacing) {  }
	}
}