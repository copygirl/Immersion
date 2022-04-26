using System;
using System.Collections.Concurrent;
using System.Linq;
using Godot;
using Immersion.Utility;
using Immersion.Voxel;
using Immersion.Voxel.Blocks;
using Immersion.Voxel.Chunks;
using Thread = System.Threading.Thread;

public class ChunkShapeUpdater : Node
{
	private static readonly float DISTANCE_SQUARED
		= Mathf.Pow((World.CHUNK_SIMULATION_DISTANCE + 0.5F) * Chunk.LENGTH, 2);


	private readonly ConcurrentBag<ChunkPos> _markToUpdate = new();
	private readonly Thread _workerThread;

	private IWorld _world = null!;
	private IChunkManager _chunks = null!;

	private Player _player = null!;
	private Vector3 _playerPosition;


	public ChunkShapeUpdater()
	{
		_workerThread = new(Work){ Name = "Chunk Shape Updater" };
		// TODO: Stop the worker thread on _ExitTree or so.
	}

	public void MarkToUpdate(ChunkPos pos)
		=> _markToUpdate.Add(pos);

	public override void _Ready()
	{
		_world  = GetParent<World>();
		_chunks = _world.Chunks;
		_chunks.ChunkReady += chunk => MarkToUpdate(chunk.ChunkPos);
		_player = GetNode<Player>("../Player");
		_workerThread.Start();
	}

	public override void _Process(float delta)
		=> _playerPosition = _player.GlobalTransform.origin;

	public void AddShapeToChunk(Chunk chunk, Shape? shape)
	{
		if (!IsInstanceValid(chunk)) return;
		var nodeBody  = chunk.GetNodeOrNull<StaticBody>("StaticBody");
		var nodeShape = nodeBody?.GetNodeOrNull<CollisionShape>("CollisionShape");

		if (shape != null) {
			if (nodeBody == null) {
				nodeBody = new(){
					CollisionLayer = (uint)CollisionLayers.World,
					CollisionMask  = 0,
				};
				nodeShape = new();
				nodeBody.AddChild(nodeShape, true);
			}
			nodeShape!.Shape = shape;
			if (nodeBody.GetParent() == null)
				chunk.AddChild(nodeBody, true);
		} else if (nodeBody != null)
			chunk.RemoveChild(nodeBody);
	}

	private void Work()
	{
		var octree   = new ChunkedOctree<bool>(5);
		var shapeGen = new ChunkShapeGenerator();

		while (true) {
			while (_markToUpdate.TryTake(out var chunkPos)) octree.Update(chunkPos,
				(int level, ReadOnlySpan<bool> children, ref bool parent) => parent = true);

			var (px, py, pz) = _playerPosition;
			// TODO: Re-use the enumerator if position is still (roughly) the same octree wasn't modified.
			var enumerator = octree.Find(
					(level, pos, state) => {
						if (!state) return null;

						var (minX, minY, minZ) = pos << Chunk.BIT_SHIFT << level;
						var maxX = minX + (1 << Chunk.BIT_SHIFT << level);
						var maxY = minY + (1 << Chunk.BIT_SHIFT << level);
						var maxZ = minZ + (1 << Chunk.BIT_SHIFT << level);

						var dx = (px < minX) ? minX - px : (px > maxX) ? maxX - px : 0.0F;
						var dy = (py < minY) ? minY - py : (py > maxY) ? maxY - py : 0.0F;
						var dz = (pz < minZ) ? minZ - pz : (pz > maxZ) ? maxZ - pz : 0.0F;
						return dx * dx + dy * dy + dz * dz;
					},
					_playerPosition.ToChunkPos())
				.Where(x => x.Weight <= DISTANCE_SQUARED)
				.GetEnumerator();

			if (enumerator.MoveNext()) {
				var (chunkPos, _, _) = enumerator.Current;
				// TODO: Instead of passing or getting the chunk here, pass (block) data necessary.
				var chunk = _chunks.GetChunkOrNull(chunkPos);
				if (chunk != null) {
					var shape = shapeGen.Generate(chunk);
					_world.Schedule(() => AddShapeToChunk((Chunk)chunk, shape));

					octree.Update(chunkPos,
						(int level, ReadOnlySpan<bool> children, ref bool parent) => {
							var state = false;
							foreach (var childState in children)
								if (childState) state = true;
							parent = state;
						});
				}
			}

			Thread.Sleep(0);
		}
	}
}
