using System;
using System.Collections.Concurrent;
using Godot;
using Immersion.Voxel;
using Thread = System.Threading.Thread;

public interface IWorld
{
	IChunkManager Chunks { get; }

	/// <summary>
	/// Runs an action on the main game thread, either by invoking it
	/// right away if the current thread is already the main thread,
	/// or by scheduling it for execution in the next process step.
	/// </summary>
	void RunOrSchedule(Action action);

	/// <summary>
	/// Schedules an action to run on the main
	/// game thread on the next process step.
	/// </summary>
	void Schedule(Action action);
}

public class World
	: Spatial
	, IWorld
{
	public const int CHUNK_UNLOAD_DISTANCE     = 14;
	public const int CHUNK_LOAD_DISTANCE       = 12;
	public const int CHUNK_RENDER_DISTANCE     = 10;
	public const int CHUNK_SIMULATION_DISTANCE =  8;


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
		Chunks = GetNode<ChunkManager>("ChunkManager") ?? throw new Exception();

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


	public void RunOrSchedule(Action action)
	{
		if (Thread.CurrentThread == _mainThread) action();
		else Schedule(action);
	}

	public void Schedule(Action action)
		=> _scheduledActions.Enqueue(action);
}
