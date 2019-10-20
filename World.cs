using Godot;
using Immersion.Voxel;
using Immersion.Voxel.Chunks;

namespace Immersion
{
	public class World : Spatial
	{
		#pragma warning disable 8618
		public ChunkManager Chunks { get; private set; }
		#pragma warning restore 8618
		
		public override void _Ready()
		{
			var texture = GD.Load<Texture>("Resources/terrain.png");
			texture.Flags = (int)Texture.FlagsEnum.ConvertToLinear;
			
			var material = new SpatialMaterial {
				AlbedoTexture = texture,
				VertexColorUseAsAlbedo = true,
			};
			
			var atlas = new TextureAtlas<string>(
				texture.GetWidth(), texture.GetHeight(), 16);
			atlas.Add("air"  , 0, 0);
			atlas.Add("stone", 1, 0);
			atlas.Add("dirt" , 2, 0);
			atlas.Add("grass", 3, 0);
			
			var generator = new ChunkMeshGenerator(this, material, atlas);
			Chunks = new ChunkManager(this, generator);
			Chunks.OnChunkAdded   += (chunk) => AddChild((Chunk)chunk);
			Chunks.OnChunkRemoved += (chunk) => RemoveChild((Chunk)chunk);
			Chunks.Tracker.StartTracking(GetNode<Spatial>("../Player"), 12);
		}
		
		public override void _Process(float delta)
		{
			if (Engine.EditorHint) return;
			Chunks.Update();
		}
	}
}
