using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Immersion.Voxel.WorldGen;

namespace Immersion.Voxel.Chunks
{
	public class ChunkManager
	{
		private const int UNLOAD_DISTANCE = 4;
		
		private readonly Dictionary<ChunkPos, IChunk> _chunks
			= new Dictionary<ChunkPos, IChunk>();
		private readonly Dictionary<Spatial, TrackedData> _tracked
			= new Dictionary<Spatial, TrackedData>();
		private List<WeightedChunkPos>? _seenChunks;
		
		public event Action<IChunk>? OnChunkAdded;
		public event Action<IChunk>? OnChunkRemoved;
		
		public World World { get; }
		public ChunkMeshGenerator MeshGenerator { get; }
		public IWorldGenerator[] Generators { get; } = {
			new BasicWorldGenerator(),
			new SurfaceGrassGenerator(),
		};
		
		public IChunk? this[ChunkPos pos]
			=> _chunks.TryGetValue(pos, out var chunk) ? chunk : null;
		
		
		public ChunkManager(World world, ChunkMeshGenerator meshGenerator)
			=> (World, MeshGenerator) = (world, meshGenerator);
		
		
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
			
			_chunks[chunk.Position] = chunk;
			OnChunkAdded?.Invoke(chunk);
		}
		
		public void Remove(ChunkPos pos)
		{
			if (!_chunks.TryGetValue(pos, out var chunk))
				throw new InvalidOperationException(
					$"Removing missing chunk {pos}");
			
			chunk.Neighbors.Clear();
			IChunk? neighborChunk;
			foreach (var neighbor in Neighbors.ALL)
			if ((neighborChunk = this[chunk.Position - neighbor]) != null)
				neighborChunk.Neighbors[neighbor] = null;
			
			_chunks.Remove(pos);
			OnChunkRemoved?.Invoke(chunk);
		}
		
		
		public void StartTracking(Spatial obj, int chunkDistance)
		{
			_tracked.Add(obj, new TrackedData(chunkDistance));
		}
		
		public void StopTracking(Spatial obj)
		{
			if (!_tracked.Remove(obj)) throw new InvalidOperationException(
				$"The object '{obj}' isn't tracked");
		}
		
		
		public void Update()
		{
			var needsUpdate = false;
			foreach (var pair in _tracked)
				if (pair.Value.Update(pair.Key.Transform.origin))
					needsUpdate = true;
			
			if (needsUpdate) {
				_seenChunks = _tracked.Values
					.SelectMany(data => data.TrackedChunks)
					.ToLookup(weighted => weighted.Position)
					.Select(collection => collection.Max())
					.OrderBy(weighted => -weighted.Weight)
					.ToList();
				
				var tooFarChunks = new List<ChunkPos>();
				var closeChunks  = new HashSet<ChunkPos>(
					_seenChunks.Select(weighted => weighted.Position));
				foreach (var loadedChunk in _chunks.Values)
				if (!closeChunks.Contains(loadedChunk.Position))
					tooFarChunks.Add(loadedChunk.Position);
				foreach (var toRemove in tooFarChunks)
					Remove(toRemove);
			}
			
			if (_seenChunks != null) {
				var chunksToGenerate = 4;
				var meshesToGenerate = 4;
				foreach (var weightedPos in _seenChunks) {
					if (weightedPos.Weight < 0) break;
					var chunk = this[weightedPos];
					if (((chunk == null) || (chunk.State < ChunkState.Generated))
					 && (chunksToGenerate > 0)) {
						chunk = GetOrCreate(weightedPos);
						chunk.State = ChunkState.Generating;
						var allGenerated = true;
						foreach (var generator in Generators) {
							if (chunk.AppliedGenerators.Contains(generator.Identifier)) continue;
							if (!generator.Populate(chunk)) { allGenerated = false; break; }
							else chunk.AppliedGenerators.Add(generator.Identifier);
							
						}
						if (allGenerated) {
							chunk.State = ChunkState.Generated;
							chunksToGenerate--;
						}
					}
					if ((chunk != null) && (chunk.State == ChunkState.Generated)
					 && Neighbors.FACINGS.All(n => (chunk.Neighbors[n] != null))
					 && (meshesToGenerate-- > 0)) {
						((Immersion.Chunk)chunk).GenerateMesh(MeshGenerator);
						chunk.State = ChunkState.Ready;
					}
				}
			}
		}
		
		
		private class TrackedData
		{
			public readonly float _maxDistanceSqr;
			public int Distance { get; }
			public ChunkPos? PreviousPos { get; private set; }
			public HashSet<WeightedChunkPos> TrackedChunks { get; }
				= new HashSet<WeightedChunkPos>();
			
			public TrackedData(int distance)
			{
				Distance = distance;
				_maxDistanceSqr = Mathf.Pow(distance + UNLOAD_DISTANCE + 0.5F, 3);
			}
			
			public bool Update(Vector3 position)
			{
				var currentPos = position.ToChunkPos();
				if (currentPos == PreviousPos) return false;
				PreviousPos = currentPos;
				
				TrackedChunks.Clear();
				for (var x = -Distance - UNLOAD_DISTANCE; x <= Distance + UNLOAD_DISTANCE; x++)
				for (var y = -Distance - UNLOAD_DISTANCE; y <= Distance + UNLOAD_DISTANCE; y++)
				for (var z = -Distance - UNLOAD_DISTANCE; z <= Distance + UNLOAD_DISTANCE; z++) {
					var distanceSqr = (x * x) + (y * y) + (z * z);
					if (distanceSqr <= _maxDistanceSqr)
						TrackedChunks.Add(new WeightedChunkPos(
							currentPos.Add(x, y, z),
							Distance - Mathf.Sqrt(distanceSqr)));
				}
				return true;
			}
		}
		
		private struct WeightedChunkPos
			: IEquatable<WeightedChunkPos>
			, IComparable<WeightedChunkPos>
		{
			public ChunkPos Position { get; }
			public float Weight { get; }
			
			public WeightedChunkPos(ChunkPos pos, float weight)
				=> (Position, Weight) = (pos, weight);
			
			public override int GetHashCode()
				=> Position.GetHashCode();
			
			public bool Equals(WeightedChunkPos other)
				=> (Position == other.Position);
			public override bool Equals(object obj)
				=> (obj is WeightedChunkPos) && Equals((WeightedChunkPos)obj);
			
			public int CompareTo(WeightedChunkPos other)
				=> Weight.CompareTo(other.Weight);
			
			public static implicit operator ChunkPos(WeightedChunkPos pos)
				=> pos.Position;
		}
	}
}
