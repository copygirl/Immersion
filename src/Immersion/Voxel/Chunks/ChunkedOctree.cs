using System;
using System.Collections;
using System.Collections.Generic;
using Immersion.Utility;

namespace Immersion.Voxel.Chunks
{
	[Flags]
	public enum ChunkState : byte
	{
		ExistsSome      = 0b00000001,
		ExistsAll       = 0b00000011,
		TrackedSome     = 0b00000100,
		TrackedAll      = 0b00001100,
		GeneratedSome   = 0b00010000,
		GeneratedAll    = 0b00110000,
		MeshUpdatedSome = 0b01000000,
		MeshUpdatedAll  = 0b11000000,
	}

	public class ChunkedOctree<T>
		where T : struct
	{
		public delegate void UpdateAction(ref T value);
		public delegate bool BubbleFunc(int level, ReadOnlySpan<T> children, ref T parent);

		public delegate float? WeightFunc(int level, ZOrder pos, T value);

		private static readonly int[] START_INDEX_LOOKUP = {
			0, 1, 9, 73, 585, 4681, 37449, 299593, 2396745, 19173961, 153391689 };


		private readonly Dictionary<ZOrder, T[]> _regions = new();

		public int Depth { get; }

		public ChunkedOctree(int depth)
		{
			if (depth < 1) throw new ArgumentOutOfRangeException(nameof(depth),
				$"{nameof(depth)} must be larger than 0");
			if (depth >= START_INDEX_LOOKUP.Length) throw new ArgumentOutOfRangeException(nameof(depth),
				$"{nameof(depth)} must be smaller than {START_INDEX_LOOKUP.Length}");
			Depth = depth;
		}

		public T Get(ChunkPos pos) => Get(0, new(pos.X, pos.Y, pos.Z));
		public T Get(int level, ZOrder pos)
		{
			var region = _regions.GetOrNull(pos >> Depth);
			if (region == null) return default;

			var baseIndex  = START_INDEX_LOOKUP[Depth - level];
			var localIndex = (long)pos & ~(~0 << (Depth * 3));
			return region[baseIndex + localIndex];
		}

		public void Update(ChunkPos pos, UpdateAction update, BubbleFunc bubble)
		{
			var zPos      = new ZOrder(pos.X, pos.Y, pos.Z);
			var regionPos = zPos >> Depth;
			var localPos  = (ZOrder)((long)zPos & ~(~0 << (Depth * 3)));

			var region = _regions.GetOrAdd(regionPos,
				() => new T[START_INDEX_LOOKUP[Depth + 1] + 1]);

			var index = START_INDEX_LOOKUP[Depth] + (long)localPos;
			update(ref region[index]);

			for (var level = 1; level <= Depth; level++) {
				var childrenBegin = START_INDEX_LOOKUP[Depth - (level - 1)];
				var childrenIndex = childrenBegin + (int)((long)localPos & ~0b111);

				localPos >>= 1;
				var parentBegin = START_INDEX_LOOKUP[Depth - level];
				var parentIndex = parentBegin + (int)(long)localPos;

				var children   = region.AsSpan(childrenIndex, 8);
				ref var parent = ref region[parentIndex];

				if (!bubble(level, children, ref parent)) break;
			}
		}

		public IEnumerable<(ChunkPos, T, float)> Find(
			WeightFunc weight, params ChunkPos[] searchFrom)
		{
			var enumerator = new Enumerator(this, weight);
			foreach (var pos in searchFrom) enumerator.SearchFrom(new(pos.X, pos.Y, pos.Z));
			while (enumerator.MoveNext()) yield return enumerator.Current;
		}

		public struct Enumerator : IEnumerator<(ChunkPos, T, float)>
		{
			private readonly ChunkedOctree<T> _octree;
			private readonly WeightFunc _weight;

			private readonly HashSet<ZOrder> _checkedRegions = new();
			private readonly PriorityQueue<(int Level, ZOrder Pos, T Value), float> _processing = new();

			internal Enumerator(ChunkedOctree<T> octree, WeightFunc weight)
				{ _octree = octree; _weight = weight; }

			public (ChunkPos, T, float) Current { get; private set; } = default;
			object IEnumerator.Current => Current;

			public bool MoveNext()
			{
				while (_processing.TryDequeue(out var element, out var weight)) {
					var (level, nodePos, value) = element;
					if (level == 0) {
						Current = (new(nodePos.X, nodePos.Y, nodePos.Z), value, weight);
						return true;
					} else for (var i = 0; i < 8; i++)
						PushNode(level - 1, (nodePos << 1) | (ZOrder)i);
				}
				return false;
			}

			public void Reset() => throw new NotSupportedException();
			public void Dispose() {  }


			internal void SearchFrom(ZOrder nodePos)
			{
				var regionPos = nodePos >> _octree.Depth;
				for (var x = -1; x <= 1; x++)
				for (var y = -1; y <= 1; y++)
				for (var z = -1; z <= 1; z++)
					SearchRegion(regionPos + new ZOrder(x, y, z));
			}

			private void SearchRegion(ZOrder regionPos)
			{
				if (_checkedRegions.Add(regionPos))
					PushNode(_octree.Depth, regionPos);
			}

			private void PushNode(int level, ZOrder nodePos)
			{
				var value = _octree.Get(level, nodePos);
				if (_weight(level, nodePos, value) is float weight)
					_processing.Enqueue((level, nodePos, value), weight);
			}
		}
	}
}
