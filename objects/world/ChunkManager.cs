using System;
using System.Collections.Concurrent;
using Godot;
using Immersion.Utility;
using Immersion.Voxel;
using Immersion.Voxel.Chunks;

public interface IChunkManager
{
	event Action<IChunk> ChunkCreated;
	event Action<IChunk> ChunkReady;
	event Action<IChunk> ChunkRemoved;

	IChunk? GetChunkOrNull(ChunkPos pos);
	IChunk GetChunkOrCreate(ChunkPos pos);

	bool TryRemoveChunk(ChunkPos pos);
	void RemoveChunk(ChunkPos pos);
}

public class ChunkManager
	: Spatial
	, IChunkManager
{
	private readonly ConcurrentDictionary<ChunkPos, Chunk> _chunks = new();

	private IWorld _world = null!;

	public event Action<IChunk>? ChunkCreated;
	public event Action<IChunk>? ChunkReady;
	public event Action<IChunk>? ChunkRemoved;

	public override void _Ready()
	{
		// TODO: Create a custom exception to use instead of IOE.
		_world = GetParent<World>() ?? throw new Exception();
	}

	public IChunk? GetChunkOrNull(ChunkPos pos)
		=> _chunks.GetOrNull(pos);
	public IChunk GetChunkOrCreate(ChunkPos pos)
		=> _chunks.GetOrAdd(pos, pos => {
			var chunk = new Chunk(_world, pos);
			foreach (var neighbor in Neighbors.ALL)
			if (GetChunkOrNull(chunk.ChunkPos + neighbor) is Chunk neighborChunk) {
				chunk.Neighbors[neighbor] = neighborChunk;
				neighborChunk.Neighbors[neighbor.GetOpposite()] = chunk;
			}
			_world.RunOrSchedule(() => {
				AddChild(chunk);
				ChunkCreated?.Invoke(chunk);
			});
			return chunk;
		});

	public bool TryRemoveChunk(ChunkPos pos)
	{
		if (!_chunks.TryRemove(pos, out var chunk)) return false;

		foreach (var neighbor in chunk.Neighbors) {
			var (x, y, z) = neighbor.ChunkPos - chunk.ChunkPos;
			neighbor.Neighbors[x, y, z] = null;
		}
		chunk.Neighbors.Clear();

		_world.RunOrSchedule(() => {
			RemoveChild(chunk);
			ChunkRemoved?.Invoke(chunk);
		});
		return true;
	}
	public void RemoveChunk(ChunkPos pos)
	{
		if (!TryRemoveChunk(pos))
			throw new InvalidOperationException(
				$"Removing missing chunk {pos}");
	}

	internal void InvokeChunkReady(Chunk chunk)
		=> ChunkReady?.Invoke(chunk);
}
