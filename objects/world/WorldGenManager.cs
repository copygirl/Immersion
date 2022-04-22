using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Immersion.Utility;
using Immersion.Voxel;
using Immersion.Voxel.Chunks;
using Immersion.Voxel.WorldGen;
using Thread = System.Threading.Thread;

public interface IWorldGenManager
{
	IReadOnlyCollection<IWorldGenerator> Generators { get; }
}

// TODO: This has to probably be worked into / split into a chunk de/serializer.
public class WorldGenManager : Node, IWorldGenManager
{
	private const int CHUNK_DISTANCE = 10;
	private const float DISTANCE_SQUARED =
		(CHUNK_DISTANCE + 0.5F) * 16 *
		(CHUNK_DISTANCE + 0.5F) * 16;


	private readonly Thread _workerThread;
	private World _world = null!;
	private Player _player = null!;
	private IChunkManager _chunks = null!;

	private Vector3 _playerPosition;


	public IReadOnlyCollection<IWorldGenerator> Generators { get; }
		= new IWorldGenerator[]{
			new BasicWorldGenerator(),
			new SurfaceGrassGenerator(),
		};


	public WorldGenManager()
	{
		_workerThread = new(Work){ Name = "World Generation" };
		// TODO: Stop the worker thread on _ExitTree or so.
	}

	public override void _Ready()
	{
		_world  = GetParent<World>();
		_player = _world.GetNode<Player>("Player");
		_chunks = _world.Chunks;
		_workerThread.Start();
	}

	public override void _Process(float delta)
		=> _playerPosition = _player.GlobalTransform.origin;

	public void Work()
	{
		var octree = new ChunkedOctree<State>(5);

		while (true) {
			var (px, py, pz) = _playerPosition;
			// TODO: Re-use the enumerator if position is still (roughly) the same octree wasn't modified.
			var enumerator = octree.Find(
					(level, pos, state) => {
						if ((state & State.GeneratedAll) == State.GeneratedAll) return null;

						var (minX, minY, minZ) = pos << level << 4;
						var maxX = minX + (1 << 4 << level);
						var maxY = minY + (1 << 4 << level);
						var maxZ = minZ + (1 << 4 << level);

						var dx = (px < minX) ? minX - px : (px > maxX) ? maxX - px : 0.0F;
						var dy = (py < minY) ? minY - py : (py > maxY) ? maxY - py : 0.0F;
						var dz = (pz < minZ) ? minZ - pz : (pz > maxZ) ? maxZ - pz : 0.0F;
						return dx * dx + dy * dy + dz * dz;
					},
					_playerPosition.ToChunkPos())
				.Where(x => x.Weight <= DISTANCE_SQUARED)
				.GetEnumerator();

			while (enumerator.MoveNext()) {
				var (chunkPos, _, _) = enumerator.Current;
				var chunk = _chunks.GetChunkOrCreate(chunkPos);

				// TODO: This is cobbled together. Would not work if chunks are created outside of this class.
				if (chunk.NumGeneratedNeighbors < 0)
					chunk.NumGeneratedNeighbors = Neighbors.ALL.Select(n => chunkPos + n)
						.Count(pos => (octree.Get(pos) & State.GeneratedAll) == State.GeneratedAll);

				// Find the generator that needs to be applied next.
				var generator = Generators
					.Where(g => !chunk.AppliedGenerators.Contains(g.Identifier))
					.FirstOrNull();

				if (generator != null) {

					// TODO: Instead of skipping this chunk if dependent neighboring chunks
					//       haven't been generated, immediately start to generate that chunk.
					//       In the same line, generate as much of the chunk as possible?
					if (!generator.NeighborDependencies.All(
						dep => _chunks.GetChunkOrNull(chunkPos + dep.Neighbor)
							?.AppliedGenerators.Contains(dep.Generator) == true))
						continue;

					// TODO: This doesn't seem safe to run in a thread?
					generator.Populate(chunk);
					chunk.AppliedGenerators.Add(generator.Identifier);
					break;

				} else {

					// If no generator was found, the chunk has completed generating.
					octree.Update(chunkPos,
						(int level, ReadOnlySpan<State> children, ref State parent) => {
							var mask = State.GeneratedAll;
							foreach (var childState in children)
								if ((childState & State.GeneratedAll) != State.GeneratedAll)
									{ mask = State.GeneratedSome; break; }
							parent |= mask;
						});

					foreach (var neighbor in chunk.Neighbors)
						if (++neighbor.NumGeneratedNeighbors == Neighbors.ALL.Count)
							_world.Schedule(() => ((ChunkManager)_chunks).InvokeChunkReady(neighbor));

					break;

				}

			}

			Thread.Sleep(0);
		}
	}

	[Flags]
	private enum State
	{
		GeneratedSome = 0b00000001,
		GeneratedAll  = 0b00000011,
	}
}
