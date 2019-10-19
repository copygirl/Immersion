using System;
using System.Collections.Generic;
using System.Linq;
using Immersion.Utility;
using Immersion.Voxel.WorldGen;

namespace Immersion.Voxel.Chunks
{
	public class ChunkManager
	{
		private readonly Dictionary<ChunkPos, IChunk> _chunks
			= new Dictionary<ChunkPos, IChunk>();
		
		
		public event Action<IChunk>? OnChunkAdded;
		public event Action<IChunk>? OnChunkRemoved;
		
		public World World { get; }
		public ChunkMeshGenerator MeshGen { get; }
		public ChunkTracker Tracker { get; }
		
		public IEnumerable<IWorldGenerator> Generators { get; }
			= new IWorldGenerator[]{
				new BasicWorldGenerator(),
				new SurfaceGrassGenerator(),
			};
		
		public IChunk? this[ChunkPos pos]
			=> _chunks.GetOrNull(pos);
		
		
		public ChunkManager(World world, ChunkMeshGenerator meshGen)
		{
			World   = world;
			MeshGen = meshGen;
			Tracker = new ChunkTracker(this);
		}
		
		
		public IChunk GetOrCreate(ChunkPos pos)
		{
			if (!_chunks.TryGetValue(pos, out var chunk)) {
				chunk = new Immersion.Chunk(World, pos);
				Add(chunk);
			}
			return chunk;
		}
		
		public void Add(IChunk chunk)
		{
			if (_chunks.ContainsKey(chunk.Position))
				throw new InvalidOperationException(
					$"Adding duplicate chunk at {chunk.Position}");
			
			IChunk? neighborChunk;
			foreach (var neighbor in Neighbors.ALL)
			if ((neighborChunk = this[chunk.Position + neighbor]) != null) {
				chunk.Neighbors[neighbor] = neighborChunk;
				neighborChunk.Neighbors[neighbor.GetOpposite()] = chunk;
			}
			
			_chunks.Add(chunk.Position, chunk);
			OnChunkAdded?.Invoke(chunk);
		}
		
		public void Remove(ChunkPos pos)
		{
			if (!TryRemove(pos))
				throw new InvalidOperationException(
					$"Removing missing chunk {pos}");
		}
		public bool TryRemove(ChunkPos pos)
		{
			if (!_chunks.TryGetValue(pos, out var chunk)) return false;
			
			chunk.Neighbors.Clear();
			IChunk? neighborChunk;
			foreach (var neighbor in Neighbors.ALL)
			if ((neighborChunk = this[chunk.Position - neighbor]) != null)
				neighborChunk.Neighbors[neighbor] = null;
			
			_chunks.Remove(pos);
			OnChunkRemoved?.Invoke(chunk);
			return true;
		}
		
		
		public void Update()
		{
			Tracker.Update();
			var requestedChunks = Tracker.SimulationRequestedChunks.Take(8).ToArray();
			foreach (var pos in requestedChunks) {
				var chunk = GetOrCreate(pos);
				switch (chunk.State) {
					case ChunkState.New:
						GenerateChunk(chunk);
						break;
					case ChunkState.Prepared:
						var allNeighborsPrepared = true;
						foreach (var neighbor in Neighbors.ALL) {
							var neighborChunk = GetOrCreate(chunk.Position + neighbor);
							if (neighborChunk.State < ChunkState.Prepared) {
								allNeighborsPrepared = false;
								GenerateChunk(neighborChunk);
								break;
							}
						}
						if (allNeighborsPrepared) {
							((Chunk)chunk).GenerateMesh(MeshGen);
							((Chunk)chunk).State = ChunkState.Ready;
							Tracker.MarkChunkReady(pos);
						}
						break;
					case ChunkState.Ready:
						Tracker.MarkChunkReady(pos);
						break;
				}
			}
		}
		
		private void GenerateChunk(IChunk chunk, string? dependency = null)
		{
			foreach (var generator in Generators) {
				if (!chunk.AppliedGenerators.Contains(generator.Identifier)) {
					foreach (var (neighbor, dep) in generator.NeighborDependencies) {
						var neighborChunk = GetOrCreate(chunk.Position + neighbor);
						if (!neighborChunk.AppliedGenerators.Contains(dep))
							GenerateChunk(neighborChunk, dep);
					}
					generator.Populate(World, chunk);
					chunk.AppliedGenerators.Add(generator.Identifier);
				}
				if (generator.Identifier == dependency) return;
			}
			((Chunk)chunk).State = ChunkState.Prepared;
		}
	}
}
