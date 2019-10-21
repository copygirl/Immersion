using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Immersion.Utility;
using Immersion.Voxel.WorldGen;
using Thread = System.Threading.Thread;

namespace Immersion.Voxel.Chunks
{
	public class ChunkManager
	{
		private readonly Dictionary<ChunkPos, ChunkData> _chunks
			= new Dictionary<ChunkPos, ChunkData>();
		
		private readonly Material _material;
		private readonly TextureAtlas<string> _textureAtlas;
			
		private readonly HashSet<ChunkPos> _workingChunks
			= new HashSet<ChunkPos>();
		private readonly List<(ChunkPos, Mesh?, Shape?)> _finishedChunks
			= new List<(ChunkPos, Mesh?, Shape?)>();
		private readonly Thread[] _workerThreads;
		
		public event Action<IChunk>? OnChunkCreated;
		public event Action<IChunk>? OnChunkFinished;
		public event Action<IChunk>? OnChunkRemoved;
		
		public World World { get; }
		public ChunkTracker Tracker { get; }
		
		public IEnumerable<IWorldGenerator> Generators { get; }
			= new IWorldGenerator[]{
				new BasicWorldGenerator(),
				new SurfaceGrassGenerator(),
			};
		
		public IChunk? this[ChunkPos pos] { get {
			lock (_chunks) return _chunks.GetOrNull(pos)?.Chunk;
		} }
		
		
		public ChunkManager(World world, Material material,
		                    TextureAtlas<string> textureAtlas)
		{
			World         = world;
			_material     = material;
			_textureAtlas = textureAtlas;
			
			Tracker = new ChunkTracker();
			Tracker.OnChunkLostTracking += (pos) => TryRemove(pos);
			
			_workerThreads = Enumerable.Range(0, 2).Select(i => new Thread(Work)).ToArray();
			foreach (var worker in _workerThreads) worker.Start();
			
			new Thread(() => { while (true) Tracker.Update(); }).Start();
		}
		
		
		public IChunk GetOrCreate(ChunkPos pos)
			=> GetOrCreateInternal(pos).Chunk;
		private ChunkData GetOrCreateInternal(ChunkPos pos)
		{
			lock (_chunks) {
				if (_chunks.TryGetValue(pos, out var data)) return data;
				
				var chunk = new Chunk(World, pos);
				IChunk? neighborChunk;
				foreach (var neighbor in Neighbors.ALL)
				if ((neighborChunk = this[chunk.Position + neighbor]) != null) {
					chunk.Neighbors[neighbor] = neighborChunk;
					neighborChunk.Neighbors[neighbor.GetOpposite()] = chunk;
				}
				
				_chunks.Add(chunk.Position, data = new ChunkData(chunk));
				OnChunkCreated?.Invoke(chunk);
				return data;
			}
		}
		
		public void Remove(ChunkPos pos)
		{
			if (!TryRemove(pos))
				throw new InvalidOperationException(
					$"Removing missing chunk {pos}");
		}
		public bool TryRemove(ChunkPos pos)
		{
			lock (_chunks) {
				if (!_chunks.TryGetValue(pos, out var data)) return false;
				
				data.Chunk.Neighbors.Clear();
				IChunk? neighborChunk;
				foreach (var neighbor in Neighbors.ALL)
				if ((neighborChunk = this[data.Chunk.Position - neighbor]) != null)
					neighborChunk.Neighbors[neighbor] = null;
				
				_chunks.Remove(pos);
				if (data.IsFinished)
					OnChunkRemoved?.Invoke(data.Chunk);
				return true;
			}
		}
		
		
		public void Update()
		{
			lock (_finishedChunks) {
				foreach (var (pos, mesh, shape) in _finishedChunks) {
					var chunk = (Chunk)this[pos]!;
					if (mesh != null)
						chunk.AddChild(new MeshInstance { Mesh = mesh });
					if (shape != null) {
						var body = new StaticBody();
						body.AddChild(new CollisionShape { Shape = shape });
						chunk.AddChild(body);
					}
					OnChunkFinished?.Invoke(chunk);
				}
				_finishedChunks.Clear();
			}
		}
		
		public void Work()
		{
			var meshGen  = new ChunkMeshGenerator(_material, _textureAtlas);
			var shapeGen = new ChunkShapeGenerator();
			
			while (true) {
				ChunkPos? nextChunkPos = null;
				lock (_workingChunks)
				lock (Tracker.SynchronizationObject) {
					var enumerator = Tracker.SimulationRequestedChunks.GetEnumerator();
					while (enumerator.MoveNext()) {
						var p = enumerator.Current;
						if (_workingChunks.Contains(p)) continue;
						lock (_chunks) if (_chunks.GetOrNull(p)?.IsFinished == true) continue;
						
						nextChunkPos = p;
						_workingChunks.Add(p);
						break;
					}
				}
				if (nextChunkPos == null)
					{ Thread.Sleep(0); continue; }
				
				var pos   = nextChunkPos.Value;
				var data  = GetOrCreateInternal(pos);
				var ready = GenerateChunk(data);
					
				var allNeighborsReady = true;
				foreach (var neighbor in Neighbors.ALL) {
					var neighborPos  = pos + neighbor;
					var neighborData = GetOrCreateInternal(neighborPos);
					if (!TryDoChunkWork(neighborPos, () => GenerateChunk(neighborData)))
						allNeighborsReady = false;
				}
				
				if (ready && allNeighborsReady) {
					var mesh  = meshGen.Generate(data.Chunk);
					var shape = shapeGen.Generate(data.Chunk);
					lock (_finishedChunks)
						_finishedChunks.Add((pos, mesh, shape));
					lock (_chunks)
						_chunks[pos].IsFinished = true;
					Tracker.MarkChunkReady(pos);
				}
				
				lock (_workingChunks)
					_workingChunks.Remove(pos);
			}
		}
		
		private bool GenerateChunk(ChunkData data, string? dependency = null)
		{
			var chunk = data.Chunk;
			foreach (var generator in Generators) {
				if (!chunk.AppliedGenerators.Contains(generator.Identifier)) {
					foreach (var (neighbor, dep) in generator.NeighborDependencies) {
						var neighborPos  = chunk.Position + neighbor;
						var neighborData = GetOrCreateInternal(neighborPos);
						
						lock (neighborData.Chunk.AppliedGenerators)
						if (neighborData.Chunk.AppliedGenerators.Contains(dep)) continue;
						
						if (!TryDoChunkWork(neighborPos, () => GenerateChunk(neighborData, dep)))
							return false;
					}
					generator.Populate(World, chunk);
					lock (chunk.AppliedGenerators)
						chunk.AppliedGenerators.Add(generator.Identifier);
				}
				if (generator.Identifier == dependency) return true;
			}
			data.IsGenerated = true;
			return true;
		}
		
		private bool TryDoChunkWork(ChunkPos pos, Func<bool> action)
		{
			lock (_workingChunks) {
				if (_workingChunks.Contains(pos)) return false;
				else _workingChunks.Add(pos);
			}
			var success = action();
			lock (_workingChunks)
				_workingChunks.Remove(pos);
			return success;
		}
		
		
		private class ChunkData
		{
			public IChunk Chunk { get; }
			public bool IsGenerated { get; set; }
			public bool IsFinished { get; set; }
			
			public ChunkData(Chunk chunk)
				=> Chunk = chunk;
		}
	}
}
