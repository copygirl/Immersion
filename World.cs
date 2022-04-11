using System;
using System.Collections.Concurrent;
using Godot;
using Immersion.Voxel;
using Immersion.Voxel.Chunks;

public class World : Spatial
{
	private readonly ConcurrentQueue<Action> _scheduledTasks = new();

	public ChunkManager Chunks { get; private set; } = null!;

	public override void _Ready()
	{
		var texture = GD.Load<Texture>("Resources/terrain.png");
		texture.Flags = (int)Texture.FlagsEnum.ConvertToLinear;

		var material = new SpatialMaterial {
			AlbedoTexture = texture,
			VertexColorUseAsAlbedo = true,
		};

		var atlas = new TextureAtlas<string>(
			texture.GetWidth(), texture.GetHeight(), 16
		){
			{ "air"  , 0, 0 },
			{ "stone", 1, 0 },
			{ "dirt" , 2, 0 },
			{ "grass", 3, 0 },
		};

		Chunks = new(this, material, atlas);
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
