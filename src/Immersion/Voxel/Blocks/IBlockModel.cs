using System.Collections.Generic;
using Godot;

namespace Immersion.Voxel.Blocks
{
	public interface IBlockModel
	{
		void RenderIntoMesh(IBlock block, BlockPos pos,
			TextureAtlas<string> textureAtlas,
			SurfaceTool st, IBlock[] neighborsByFacing);

		void AddCollisionShape(IBlock block, BlockPos pos,
			List<Vector3> triangles, IBlock[] neighborsByFacing);
	}
}
