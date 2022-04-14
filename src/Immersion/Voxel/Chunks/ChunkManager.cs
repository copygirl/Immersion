using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Immersion.Utility;
using Immersion.Voxel.WorldGen;

namespace Immersion.Voxel.Chunks
{
	public class ChunkManager : Node
	{
		private const int CHUNK_RENDER_DISTANCE = 4;

		private const float MAX_DISTANCE_SQUARED =
			(CHUNK_RENDER_DISTANCE + 0.5F) * 16 *
			(CHUNK_RENDER_DISTANCE + 0.5F) * 16;


		private readonly Dictionary<ChunkPos, Chunk> _chunks = new();
		private readonly ChunkedOctree<ChunkState> _octree = new(5);

		private readonly ChunkMeshGenerator _meshGen;
		private readonly ChunkShapeGenerator _shapeGen = new();

		public World World { get; }

		public IEnumerable<IWorldGenerator> Generators { get; }
			= new IWorldGenerator[]{
				new BasicWorldGenerator(),
				new SurfaceGrassGenerator(),
			};


		public ChunkManager(World world, Material material,
		                    TextureAtlas<string> textureAtlas)
		{
			World    = world;
			_meshGen = new(material, textureAtlas);
		}

		public Chunk? GetOrNull(ChunkPos pos)
			=> _chunks.GetOrNull(pos);
		public Chunk GetOrCreate(ChunkPos pos)
		{
			if (_chunks.TryGetValue(pos, out var chunk)) return chunk;

			chunk = new Chunk(World, pos);
			Chunk? neighborChunk;
			foreach (var neighbor in Neighbors.ALL)
			if ((neighborChunk = GetOrNull(chunk.Position + neighbor)) != null) {
				chunk.Neighbors[neighbor] = neighborChunk;
				neighborChunk.Neighbors[neighbor.GetOpposite()] = chunk;
			}

			_chunks.Add(chunk.Position, chunk);
			World.AddChild(chunk);
			return chunk;
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
			Chunk? neighborChunk;
			foreach (var neighbor in Neighbors.ALL)
			if ((neighborChunk = GetOrNull(chunk.Position - neighbor)) != null)
				neighborChunk.Neighbors[neighbor] = null;

			_chunks.Remove(pos);
			World.RemoveChild(chunk);
			return true;
		}


		public void ForceUpdate(Chunk chunk)
		{
			var mesh  = _meshGen.Generate(chunk);
			var shape = _shapeGen.Generate(chunk);
			SetChunkMeshAndShape(chunk, mesh, shape);
		}

		private void SetChunkMeshAndShape(Chunk chunk, Mesh? mesh, Shape? shape)
		{
			var hasMesh = chunk.HasNode("MeshInstance");
			var hasBody = chunk.HasNode("StaticBody");
			var meshNode  = hasMesh ? chunk.GetNode<MeshInstance>("MeshInstance") : null;
			var bodyNode  = hasBody ? chunk.GetNode<StaticBody>("StaticBody") : null;
			var shapeNode = bodyNode?.GetNode<CollisionShape>("CollisionShape");

			if (mesh != null) {
				if (!hasMesh) meshNode = new();
				meshNode!.Mesh = mesh;
				if (!hasMesh)
					chunk.AddChild(meshNode, true);
			} else if (meshNode != null)
				chunk.RemoveChild(meshNode);

			if (shape != null) {
				if (!hasBody) {
					bodyNode = new(){
						CollisionLayer = (uint)CollisionLayers.World,
						CollisionMask  = 0,
					};
					shapeNode = new();
					bodyNode.AddChild(shapeNode, true);
				}
				shapeNode!.Shape = shape;
				if (!hasBody)
					chunk.AddChild(bodyNode, true);
			} else if (bodyNode != null)
				chunk.RemoveChild(bodyNode);

			_octree.Update(chunk.Position,
				(ref ChunkState state) => state |= ChunkState.MeshUpdatedAll,
				(int level, ReadOnlySpan<ChunkState> children, ref ChunkState parent) => {
					var mask = ChunkState.MeshUpdatedAll;
					foreach (var childState in children)
						if ((childState & ChunkState.MeshUpdatedAll) != ChunkState.MeshUpdatedAll)
							{ mask = ChunkState.MeshUpdatedSome; break; }
					if ((parent & mask) == mask) return false;
					else { parent &= mask; return true; }
				});
		}

		public override void _Process(float delta)
		{
			var player       = GetNode<Player>("/root/World/Player");
			var (px, py, pz) = player.GlobalTransform.origin;

			var toProcess = _octree.Find(
				(level, pos, state) => {
					if ((state & (ChunkState.GeneratedAll | ChunkState.MeshUpdatedAll))
						== (ChunkState.GeneratedAll | ChunkState.MeshUpdatedAll))
							return null;

					var (minX, minY, minZ) = pos << level << 4;
					var maxX = minX + (1 << 4 << level);
					var maxY = minY + (1 << 4 << level);
					var maxZ = minZ + (1 << 4 << level);

					var dx = (px < minX) ? minX - px : (px > maxX) ? maxX - px : 0.0F;
					var dy = (py < minY) ? minY - py : (py > maxY) ? maxY - py : 0.0F;
					var dz = (pz < minZ) ? minZ - pz : (pz > maxZ) ? maxZ - pz : 0.0F;
					return dx * dx + dy * dy + dz * dz;
				},
				player.GlobalTransform.origin.ToChunkPos());

			var numToProcess = 4;
			foreach (var (chunkPos, state, distanceSqr) in toProcess) {
				if (distanceSqr > MAX_DISTANCE_SQUARED) break;

				var chunk = GetOrCreate(chunkPos);

				if ((state & ChunkState.GeneratedAll) != ChunkState.GeneratedAll) {

					var generator = Generators
						.Where(g => !chunk.AppliedGenerators.Contains(g.Identifier))
						.FirstOrNull();

					if (generator == null) {
						_octree.Update(chunkPos,
							(ref ChunkState state) => state |= ChunkState.GeneratedAll,
							(int level, ReadOnlySpan<ChunkState> children, ref ChunkState parent) => {
								var mask = ChunkState.GeneratedAll;
								foreach (var childState in children)
									if ((childState & ChunkState.GeneratedAll) != ChunkState.GeneratedAll)
										{ mask = ChunkState.GeneratedSome; break; }
								if ((parent & mask) == mask) return false;
								else { parent &= mask; return true; }
							});
						continue;
					}

					if (!generator.NeighborDependencies.All(
						dep => GetOrNull(chunkPos + dep.Neighbor)
							?.AppliedGenerators.Contains(dep.Generator) == true))
						continue;

					generator.Populate(World, chunk);
					chunk.AppliedGenerators.Add(generator.Identifier);

					if (numToProcess-- == 0) break;

				} else if ((state & ChunkState.MeshUpdatedAll) != ChunkState.MeshUpdatedAll) {

					var neighbors = Neighbors.ALL.Select(n => chunkPos + n);
					if (!neighbors.All(pos => (_octree.Get(pos) & ChunkState.GeneratedAll) == ChunkState.GeneratedAll))
						continue;

					var mesh  = _meshGen.Generate(chunk);
					var shape = _shapeGen.Generate(chunk);
					SetChunkMeshAndShape(chunk, mesh, shape);

					if (numToProcess-- == 0) break;
				}
			}
		}
	}
}
