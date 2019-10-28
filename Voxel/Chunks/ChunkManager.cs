using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
		private readonly Thread _workerThread;
		
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
			
			_workerThread = new Thread(Work);
			_workerThread.Start();
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
				if (data.IsFinished) World.ScheduleTask(() =>
					World.RemoveChild((Chunk)data.Chunk));
				return true;
			}
		}
		
		
		public void ForceUpdate(IChunk chunk)
		{
			var meshGen  = new ChunkMeshGenerator(_material, _textureAtlas);
			var shapeGen = new ChunkShapeGenerator();
			
			Mesh?  mesh  = meshGen.Generate(chunk);
			Shape? shape = shapeGen.Generate(chunk);
			
			SetChunkMeshAndShape(chunk, mesh, shape);
		}
		
		private void SetChunkMeshAndShape(IChunk chunk, Mesh? mesh, Shape? shape)
		{
			var godotChunk = (Chunk)chunk;
			var hasMesh = godotChunk.HasNode("MeshInstance");
			var hasBody = godotChunk.HasNode("StaticBody");
			var meshNode  = hasMesh ? godotChunk.GetNode<MeshInstance>("MeshInstance") : null;
			var bodyNode  = hasBody ? godotChunk.GetNode<StaticBody>("StaticBody") : null;
			var shapeNode = bodyNode?.GetNode<CollisionShape>("CollisionShape");
			
			if (mesh != null) {
				if (!hasMesh) meshNode = new MeshInstance();
				meshNode!.Mesh = mesh;
				if (!hasMesh) godotChunk.AddChild(meshNode, true);
			} else if (meshNode != null) godotChunk.RemoveChild(meshNode);
			
			if (shape != null) {
				if (!hasBody) {
					bodyNode  = new StaticBody();
					shapeNode = new CollisionShape();
					bodyNode.AddChild(shapeNode, true);
				}
				shapeNode!.Shape = shape;
				if (!hasBody) godotChunk.AddChild(bodyNode, true);
			} else if (bodyNode != null) godotChunk.RemoveChild(bodyNode);
		}
		
		
		private void Work()
		{
			const int MAX_TASKS = 24;
			var working = new Dictionary<ChunkPos, Task<object>>();
			var generatorLookup = Generators.ToDictionary(gen => gen.Identifier);
			
			var meshShapeGenerators = new Stack<(ChunkMeshGenerator, ChunkShapeGenerator)>();
			
			while (true) {
				Tracker.Update();
				
				var finishedWork = working.Where(kvp => kvp.Value.IsCompleted).ToArray();
				foreach (var (pos, task) in finishedWork) {
					working.Remove(pos);
					switch (task.Result) {
						case IWorldGenerator generator:
							this[pos]!.AppliedGenerators.Add(generator.Identifier);
							break;
						case ValueTuple<Mesh?, Shape?, (ChunkMeshGenerator, ChunkShapeGenerator)> tuple:
							var (mesh, shape, meshShapeGen) = tuple;
							meshShapeGenerators.Push(meshShapeGen);
							Tracker.MarkChunkReady(pos);
							
							var chunk = (Chunk)this[pos]!;
							SetChunkMeshAndShape(chunk, mesh, shape);
							
							World.ScheduleTask(() =>
								World.AddChild(chunk));
							break;
					}
				}
				
				foreach (var pos in Tracker.SimulationRequestedChunks) {
					if (working.Count >= MAX_TASKS) break;
					if (working.ContainsKey(pos)) continue;
					var data  = GetOrCreateInternal(pos);
					var chunk = data.Chunk;
					
					if (!data.IsGenerated) {
						
						var nextGenerator = Generators
							.Where(gen => !chunk.AppliedGenerators.Contains(gen.Identifier))
							.FirstOrNull();
						
						if (nextGenerator == null) {
							data.IsGenerated = true;
							continue;
						}
						
						if (!nextGenerator.NeighborDependencies
							.All(((Neighbor neighbor, string dep) t)
								=> !working.ContainsKey(pos + t.neighbor)
								&& (this[pos + t.neighbor]?.AppliedGenerators.Contains(t.dep) == true)))
							continue;
						
						var task = Task.Run<object>(() => {
							nextGenerator.Populate(World, chunk);
							return nextGenerator;
						});
						working.Add(pos, task);
						
						// This is silly but it's an acceptable workaround for now.
						var neighborTask = task.ContinueWith<object>(task => "neighbor");
						foreach (var (neighbor, _) in nextGenerator.NeighborDependencies)
							working.Add(pos + neighbor, neighborTask);
						
					} else if (!data.IsFinished) {
						ChunkMeshGenerator meshGen;
						ChunkShapeGenerator shapeGen;
						
						if (meshShapeGenerators.Count == 0) {
							meshGen  = new ChunkMeshGenerator(_material, _textureAtlas);
							shapeGen = new ChunkShapeGenerator();
						} else (meshGen, shapeGen) = meshShapeGenerators.Pop();
						
						lock (_chunks)
						if (Neighbors.ALL.All(n => _chunks.GetOrNull(pos + n)?.IsGenerated == true)) {
							working.Add(pos, Task.Run<object>(() => {
								Mesh?  mesh  = meshGen.Generate(chunk);
								Shape? shape = shapeGen.Generate(chunk);
								data.IsFinished = true;
								return (mesh, shape, (meshGen, shapeGen));
							}));
						}
					}
				}
				
				Thread.Sleep(0);
			}
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
