using System;
using System.Collections.Concurrent;
using Godot;
using Immersion.Voxel;
using Immersion.Voxel.Chunks;

public class World : Spatial
{
	private readonly ConcurrentQueue<Action> _scheduledTasks = new();

	private readonly SpatialMaterial _material
		= new(){ VertexColorUseAsAlbedo = true };

	[Export] public Texture TerrainTexture {
		get => _material.AlbedoTexture;
		set => _material.AlbedoTexture = value;
	}

	public ChunkManager Chunks { get; private set; } = null!;

	public override void _Ready()
	{
		TerrainTexture.Flags = (int)Texture.FlagsEnum.ConvertToLinear;

		var (width, height) = (TerrainTexture.GetWidth(), TerrainTexture.GetHeight());
		var textureAtlas = new TextureAtlas<string>(width, height, 16){
			{ "air"  , 0, 0 },
			{ "stone", 1, 0 },
			{ "dirt" , 2, 0 },
			{ "grass", 3, 0 },
		};

		Chunks = new(this, _material, textureAtlas);
		AddChild(Chunks);
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
