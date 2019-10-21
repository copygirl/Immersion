using System;
using System.Collections.Generic;
using Godot;

namespace Immersion.Voxel.Blocks
{
	public class CubeBlockModel
		: IBlockModel, IBlockModelCullSide
	{
		public static readonly CubeBlockModel INSTANCE = new CubeBlockModel();
		
		private static readonly Vector3[] OFFSET_PER_FACING = {
			// East  (+X)
			new Vector3(1, 1, 1),
			new Vector3(1, 0, 1),
			new Vector3(1, 0, 0),
			new Vector3(1, 1, 0),
			// West  (-X)
			new Vector3(0, 1, 0),
			new Vector3(0, 0, 0),
			new Vector3(0, 0, 1),
			new Vector3(0, 1, 1),
			// Up    (+Y)
			new Vector3(1, 1, 0),
			new Vector3(0, 1, 0),
			new Vector3(0, 1, 1),
			new Vector3(1, 1, 1),
			// Down  (-Y)
			new Vector3(1, 0, 1),
			new Vector3(0, 0, 1),
			new Vector3(0, 0, 0),
			new Vector3(1, 0, 0),
			// South (+Z)
			new Vector3(0, 1, 1),
			new Vector3(0, 0, 1),
			new Vector3(1, 0, 1),
			new Vector3(1, 1, 1),
			// North (-Z)
			new Vector3(1, 1, 0),
			new Vector3(1, 0, 0),
			new Vector3(0, 0, 0),
			new Vector3(0, 1, 0),
		};
		
		private static readonly int[] TRIANGLE_INDICES
			= { 0, 3, 1,  1, 3, 2 };
		
		
		public void RenderIntoMesh(IBlock block, BlockPos pos,
			TextureAtlas<string> textureAtlas,
			SurfaceTool st, IBlock[] neighborsByFacing)
		{
			var textureCell = textureAtlas[block.Texture];
			var blockVertex = new Vector3(pos.X, pos.Y, pos.Z);
			foreach (var facing in BlockFacings.ALL) {
				var neighbor = neighborsByFacing[(int)facing];
				
				if ((neighbor.Model is IBlockModelCullSide other)
				 && CanSideCull(block, facing)
				 && other.CanSideCull(neighbor, facing.GetOpposite())) continue;
				
				var vertIndex = (int)facing << 2;
				st.AddNormal(facing.ToVector3());
				for (var i = 0; i < 6; i++) {
					var j = TRIANGLE_INDICES[i];
					Vector2 uv;
					switch (j) {
						case 0: uv = textureCell.TopLeft;     break;
						case 1: uv = textureCell.BottomLeft;  break;
						case 2: uv = textureCell.BottomRight; break;
						case 3: uv = textureCell.TopRight;    break;
						default: throw new InvalidOperationException();
					}
					st.AddUv(uv);
					st.AddVertex(blockVertex + OFFSET_PER_FACING[vertIndex | j]);
				}
			}
		}
		
		public void AddCollisionShape(IBlock block, BlockPos pos,
			List<Vector3> triangles, IBlock[] neighborsByFacing)
		{
			var blockVertex = new Vector3(pos.X, pos.Y, pos.Z);
			foreach (var facing in BlockFacings.ALL) {
				var neighbor = neighborsByFacing[(int)facing];
				
				if ((neighbor.Model is IBlockModelCullSide other)
				 && CanSideCull(block, facing)
				 && other.CanSideCull(neighbor, facing.GetOpposite())) continue;
				
				var vertIndex = (int)facing << 2;
				for (var i = 0; i < 6; i++) {
					var j = TRIANGLE_INDICES[i];
					triangles.Add(blockVertex + OFFSET_PER_FACING[vertIndex | j]);
				}
			}
		}
		
		public bool CanSideCull(IBlock block, BlockFacing facing) => true;
	}
	
	public interface IBlockModelCullSide
	{
		bool CanSideCull(IBlock block, BlockFacing facing);
	}
}
