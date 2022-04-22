using System;
using System.Collections.Concurrent;
using Godot;
using Immersion.Voxel;
using Thread = System.Threading.Thread;

// TODO: Add IWorld interface.
public class World : Spatial
{
	private readonly ConcurrentQueue<Action> _scheduledActions = new();
	private readonly Thread _mainThread = Thread.CurrentThread;

	public IChunkManager Chunks { get; private set; } = null!;


	public readonly SpatialMaterial TerrainMaterial
		= new(){ VertexColorUseAsAlbedo = true };

	[Export] public Texture TerrainTexture {
		get => TerrainMaterial.AlbedoTexture;
		set => TerrainMaterial.AlbedoTexture = value;
	}

	public TextureAtlas<string> TerrainAtlas { get; private set; } = null!;


	public override void _EnterTree()
	{
		Chunks = GetNode<ChunkManager>("ChunkManager") ?? throw new InvalidOperationException();

		TerrainTexture.Flags = (int)Texture.FlagsEnum.ConvertToLinear;

		var (width, height) = (TerrainTexture.GetWidth(), TerrainTexture.GetHeight());
		TerrainAtlas = new TextureAtlas<string>(width, height, 16){
			{ "air"  , 0, 0 },
			{ "stone", 1, 0 },
			{ "dirt" , 2, 0 },
			{ "grass", 3, 0 },
		};
	}

	public override void _Process(float delta)
	{
		while (_scheduledActions.TryDequeue(out var action))
			action();
	}

	/// <summary> Schedules an action to run on the main game thread. </summary>
	public void Schedule(Action action)
		=> _scheduledActions.Enqueue(action);

	/// <summary>
	/// Runs an action on the main game thread, either by invoking it
	/// right away if the current thread is already the main thread,
	/// or by scheduling it for execution in the next process step.
	/// </summary>
	public void RunOrSchedule(Action action)
	{
		if (Thread.CurrentThread == _mainThread) action();
		else Schedule(action);
	}
}
