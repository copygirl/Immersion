using System;
using Godot;
using Immersion.Utility;
using Immersion.Voxel.Chunks;

namespace Immersion.Tests
{
	public static class Test_ChunkedOctree
	{
		[Test]
		public static void Update()
		{
			var octree = new ChunkedOctree<bool>(3);

			Assert.False(octree.Get(new( 0,  0,  0)));
			Assert.False(octree.Get(new( 1,  1,  1)));
			Assert.False(octree.Get(new(-1, -1, -1)));

			octree.Update(new(0, 0, 0), (int level, ReadOnlySpan<bool> children, ref bool parent) => parent = true);
			Assert.True(octree.Get(new(0, 0, 0)));

			Assert.True(octree.Get(0, new(0, 0, 0)));
			Assert.True(octree.Get(1, new(0, 0, 0)));
			Assert.True(octree.Get(2, new(0, 0, 0)));
			Assert.True(octree.Get(3, new(0, 0, 0)));

			Assert.False(octree.Get(0, new(1, 1, 1)));
			Assert.False(octree.Get(1, new(2, 2, 2)));
			Assert.False(octree.Get(2, new(4, 4, 4)));
			Assert.False(octree.Get(3, new(8, 8, 8)));

			Assert.False(octree.Get(0, new(-1, -1, -1)));
			Assert.False(octree.Get(1, new(-1, -1, -1)));
			Assert.False(octree.Get(2, new(-1, -1, -1)));
			Assert.False(octree.Get(3, new(-1, -1, -1)));

			octree.Update(new(-1, -1, -1), (int level, ReadOnlySpan<bool> children, ref bool parent) => parent = true);
			Assert.True(octree.Get(new(-1, -1, -1)));

			Assert.True(octree.Get(0, new(-1, -1, -1)));
			Assert.True(octree.Get(1, new(-1, -1, -1)));
			Assert.True(octree.Get(2, new(-1, -1, -1)));
			Assert.True(octree.Get(3, new(-1, -1, -1)));
		}

		[Test]
		public static void Find()
		{
			var octree = new ChunkedOctree<bool>(3);
			octree.Update(new( 0,  0,  0), (int level, ReadOnlySpan<bool> children, ref bool parent) => parent = true);
			octree.Update(new(-1, -1, -1), (int level, ReadOnlySpan<bool> children, ref bool parent) => parent = true);
			octree.Update(new( 2,  2,  2), (int level, ReadOnlySpan<bool> children, ref bool parent) => parent = true);

			var searchFrom   = new Vector3(8, 8, 8);
			var (px, py, pz) = searchFrom;
			var enumerator = octree.Find(
					(level, pos, state) => {
						if (!state) return null;

						var (minX, minY, minZ) = pos << level << 4;
						var maxX = minX + (1 << 4 << level);
						var maxY = minY + (1 << 4 << level);
						var maxZ = minZ + (1 << 4 << level);

						var dx = (px < minX) ? minX - px : (px > maxX) ? maxX - px : 0.0F;
						var dy = (py < minY) ? minY - py : (py > maxY) ? maxY - py : 0.0F;
						var dz = (pz < minZ) ? minZ - pz : (pz > maxZ) ? maxZ - pz : 0.0F;
						return dx * dx + dy * dy + dz * dz;
					}, searchFrom.ToChunkPos())
				.GetEnumerator();

			Assert.True(enumerator.MoveNext()); var first  = enumerator.Current;
			Assert.True(enumerator.MoveNext()); var second = enumerator.Current;
			Assert.True(enumerator.MoveNext()); var third  = enumerator.Current;
			Assert.False(enumerator.MoveNext());

			Assert.Equal(new(0, 0, 0), first.ChunkPos);
			Assert.Equal(0, first.Weight);
			Assert.True(first.Value);

			Assert.Equal(new(-1, -1, -1), second.ChunkPos);
			Assert.Equal(8*8 + 8*8 + 8*8, second.Weight);
			Assert.True(second.Value);

			Assert.Equal(new(2, 2, 2), third.ChunkPos);
			Assert.Equal(24*24 + 24*24 + 24*24, third.Weight);
			Assert.True(third.Value);
		}
	}
}
