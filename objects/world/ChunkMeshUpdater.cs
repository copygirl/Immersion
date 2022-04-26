using System;
using System.Collections.Concurrent;
using System.Linq;
using Godot;
using Immersion.Utility;
using Immersion.Voxel;
using Immersion.Voxel.Blocks;
using Immersion.Voxel.Chunks;
using Thread = System.Threading.Thread;

public class ChunkMeshUpdater : Node
{
	private static readonly float DISTANCE_SQUARED
		= Mathf.Pow((World.CHUNK_RENDER_DISTANCE + 0.5F) * Chunk.LENGTH, 2);


	[Export] internal NodePath _cameraPath = null!;

	private readonly ConcurrentBag<ChunkPos> _markToUpdate = new();
	private readonly Thread _workerThread;

	private IWorld _world = null!;
	private IChunkManager _chunks = null!;

	private Camera _camera = null!;
	private Vector3 _cameraPosition;


	public ChunkMeshUpdater()
	{
		_workerThread = new(Work){ Name = "Chunk Mesh Updater" };
		// TODO: Stop the worker thread on _ExitTree or so.
	}

	public void MarkToUpdate(ChunkPos pos)
		=> _markToUpdate.Add(pos);

	public override void _Ready()
	{
		_world  = GetParent<World>();
		_chunks = _world.Chunks;
		_chunks.ChunkReady += chunk => MarkToUpdate(chunk.ChunkPos);
		_camera = GetNode<Camera>(_cameraPath) ?? throw new Exception();
		_workerThread.Start();
	}

	public override void _Process(float delta)
		=> _cameraPosition = _camera.GlobalTransform.origin;

	private void AddMeshToChunk(Chunk chunk, Mesh? mesh)
	{
		if (!IsInstanceValid(chunk)) return;
		var nodeMesh = chunk.GetNodeOrNull<MeshInstance>("MeshInstance");

		if (mesh != null) {
			nodeMesh ??= new();
			nodeMesh.Mesh = mesh;
			if (nodeMesh.GetParent() == null)
				chunk.AddChild(nodeMesh, true);
		} else if (nodeMesh != null)
			chunk.RemoveChild(nodeMesh);
	}

	private void Work()
	{
		var octree  = new ChunkedOctree<bool>(5);
		var meshGen = new ChunkMeshGenerator(GetParent<World>());

		while (true) {
			while (_markToUpdate.TryTake(out var chunkPos)) octree.Update(chunkPos,
				(int level, ReadOnlySpan<bool> children, ref bool parent) => parent = true);

			var (px, py, pz) = _cameraPosition;
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
					_cameraPosition.ToChunkPos())
				.Where(x => x.Weight <= DISTANCE_SQUARED)
				.GetEnumerator();

			if (enumerator.MoveNext()) {
				var (chunkPos, _, _) = enumerator.Current;
				// TODO: Instead of passing or getting the chunk here, pass (block) data necessary.
				var chunk = _chunks.GetChunkOrNull(chunkPos);
				if (chunk != null) {
					var mesh = meshGen.Generate(chunk);
					_world.Schedule(() => AddMeshToChunk((Chunk)chunk, mesh));

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
