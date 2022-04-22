using System;
using System.Collections;
using System.Collections.Generic;
using Immersion.Utility;

namespace Immersion.Voxel.Chunks
{
	public class ChunkedOctree<T>
		where T : struct
	{
		public delegate void UpdateAction(int level, ReadOnlySpan<T> children, ref T parent);
		public delegate float? WeightFunc(int level, ZOrder pos, T value);

		private static readonly int[] START_INDEX_LOOKUP = {
			0, 1, 9, 73, 585, 4681, 37449, 299593, 2396745, 19173961, 153391689 };


		private readonly IEqualityComparer<T> _comparer = EqualityComparer<T>.Default;
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

		public T Get(ChunkPos pos)
			=> Get(0, new(pos.X, pos.Y, pos.Z));
		public T Get(int level, ZOrder pos)
		{
			var region = _regions.GetOrNull(pos >> (Depth - level));
			if (region == null) return default;
			var localPos = pos & ~(~0L << ((Depth - level) * 3));
			return region[GetIndex(level, localPos)];
		}
		private int GetIndex(int level, ZOrder localPos)
			=> START_INDEX_LOOKUP[Depth - level] + (int)localPos.Raw;

		public void Update(ChunkPos pos, UpdateAction update)
		{
			var zPos      = new ZOrder(pos.X, pos.Y, pos.Z);
			var localPos  = zPos & ~(~0L << (Depth * 3));
			var regionPos = zPos >> Depth;

			var region = _regions.GetOrAdd(regionPos,
				() => new T[START_INDEX_LOOKUP[Depth + 1] + 1]);

			var children = default(ReadOnlySpan<T>);
			for (var level = 0; level <= Depth; level++) {
				var index = GetIndex(level, localPos);

				var previous = region[index];
				update(0, children, ref region[index]);
				if (_comparer.Equals(region[index], previous)) return;

				if (level == Depth) return;
				children = region.AsSpan(GetIndex(level, localPos & ~0b111L), 8);
				localPos >>= 1;
			}
		}

		public IEnumerable<(ChunkPos ChunkPos, T Value, float Weight)> Find(
			WeightFunc weight, params ChunkPos[] searchFrom)
		{
			var enumerator = new Enumerator(this, weight);
			foreach (var pos in searchFrom) enumerator.SearchFrom(new(pos.X, pos.Y, pos.Z));
			while (enumerator.MoveNext()) yield return enumerator.Current;
		}

		public class Enumerator : IEnumerator<(ChunkPos ChunkPos, T Value, float Weight)>
		{
			private readonly ChunkedOctree<T> _octree;
			private readonly WeightFunc _weight;

			private readonly HashSet<ZOrder> _checkedRegions = new();
			private readonly PriorityQueue<(int Level, ZOrder Pos, T Value), float> _processing = new();
			private (ChunkPos ChunkPos, T Value, float Weight)? _current;

			internal Enumerator(ChunkedOctree<T> octree, WeightFunc weight)
				{ _octree = octree; _weight = weight; _current = null; }

			public (ChunkPos ChunkPos, T Value, float Weight) Current
				=> _current ?? throw new InvalidOperationException();
			object IEnumerator.Current => Current;

			public bool MoveNext()
			{
				while (_processing.TryDequeue(out var element, out var weight)) {
					var (level, nodePos, value) = element;
					if (level == 0) {
						_current = (new(nodePos.X, nodePos.Y, nodePos.Z), value, weight);
						return true;
					} else for (var i = 0b000; i <= 0b111; i++)
						PushNode(level - 1, (nodePos << 1) | ZOrder.FromRaw(i));
				}
				_current = null;
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
