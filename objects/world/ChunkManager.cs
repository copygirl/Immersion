using System;
using System.Collections.Concurrent;
using Godot;
using Immersion.Utility;
using Immersion.Voxel;
using Immersion.Voxel.Chunks;

public interface IChunkManager
{
	event Action<Chunk> ChunkCreated;
	event Action<Chunk> ChunkReady;
	event Action<Chunk> ChunkRemoved;

	Chunk? GetChunkOrNull(ChunkPos pos);
	Chunk GetChunkOrCreate(ChunkPos pos);

	bool TryRemoveChunk(ChunkPos pos);
	void RemoveChunk(ChunkPos pos);
}

public class ChunkManager
	: Spatial
	, IChunkManager
{
	private readonly ConcurrentDictionary<ChunkPos, Chunk> _chunks = new();

	private World _world = null!;

	public event Action<Chunk>? ChunkCreated;
	public event Action<Chunk>? ChunkReady;
	public event Action<Chunk>? ChunkRemoved;

	public override void _Ready()
	{
		// TODO: Create a custom exception to use instead of IOE.
		_world = GetParent<World>() ?? throw new InvalidOperationException();
	}

	public Chunk? GetChunkOrNull(ChunkPos pos)
		=> _chunks.GetOrNull(pos);
	public Chunk GetChunkOrCreate(ChunkPos pos)
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
