using Godot;
using Immersion.Voxel;
using Immersion.Voxel.Chunks;

namespace Immersion
{
	[Tool]
	public class World : Spatial
	{
		#pragma warning disable 8618
		public ChunkManager Chunks { get; private set; }
		#pragma warning restore 8618

		[Export]
		public Texture? Texture { get; set; }
		[Export]
		public int TextureCellSize { get; set; } = 16;
		
		
		public override void _Ready()
		{
			var material = new SpatialMaterial {
				AlbedoTexture = Texture,
				VertexColorUseAsAlbedo = true,
			};
			
			var atlas = new TextureAtlas<string>(
				Texture!.GetWidth(), Texture.GetHeight(), TextureCellSize);
			atlas.Add("air"  , 0, 0);
			atlas.Add("stone", 1, 0);
			atlas.Add("dirt" , 2, 0);
			atlas.Add("grass", 3, 0);
			
			var generator = new ChunkMeshGenerator(this, material, atlas);
			Chunks = new ChunkManager(this, generator);
			Chunks.OnChunkAdded   += (chunk) => AddChild((Chunk)chunk);
			Chunks.OnChunkRemoved += (chunk) => RemoveChild((Chunk)chunk);
			Chunks.StartTracking(GetNode<Spatial>("../Player"), 12);
		}
		
		public override void _Process(float delta)
		{
			if (Engine.EditorHint) return;
			Chunks.Update();
		}
	}
}
