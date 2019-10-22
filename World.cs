using System;
using System.Collections.Concurrent;
using Godot;
using Immersion.Voxel;
using Immersion.Voxel.Chunks;

namespace Immersion
{
	public class World : Spatial
	{
		private readonly ConcurrentQueue<Action> _scheduledTasks
			= new ConcurrentQueue<Action>();
		
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
			
			Chunks = new ChunkManager(this, material, atlas);
			Chunks.Tracker.StartTracking(GetNode<Spatial>("../Player"), 12);
		}
		
		public override void _Process(float delta)
		{
			while (_scheduledTasks.TryDequeue(out var action))
				action();
		}
		
		/// <summary> Schedules an action to run on the main game thread. </summary>
		public void ScheduleTask(Action action)
			=> _scheduledTasks.Enqueue(action);
	}
}
