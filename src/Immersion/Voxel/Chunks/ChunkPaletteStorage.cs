using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Immersion.Voxel.Chunks
{
	// Based on "Palette-based compression for chunked discrete voxel data" by /u/Longor1996
	// https://www.reddit.com/r/VoxelGameDev/comments/9yu8qy/palettebased_compression_for_chunked_discrete/
	public class ChunkPaletteStorage<T>
		: IVoxelStorage<T>
			where T : class
	{
		private const int SIZE = Chunk.LENGTH * Chunk.LENGTH * Chunk.LENGTH;

		private BitArray? _data;
		private PaletteEntry[]? _palette;
		private int _usedPalettes;
		private int _indicesLength;


		public T Default { get; }

		public int Width  => Chunk.LENGTH;
		public int Height => Chunk.LENGTH;
		public int Depth  => Chunk.LENGTH;

		public T this[int x, int y, int z] {
			get => Get(x, y, z);
			set => Set(x, y, z, value);
		}

		public IEnumerable<T> Blocks
			=> _palette.Where(entry => entry.Value != null)
			           .Select(entry => entry.Value!);


		public ChunkPaletteStorage(T @default)
			=> Default = @default;


		private T Get(int x, int y, int z)
			=> _palette?[GetPaletteIndex(x, y, z)].Value ?? Default;

		private void Set(int x, int y, int z, T value)
		{
			if (_palette == null) {
				if (value == Default) return;
			} else {
				var index = GetIndex(x, y, z);
				ref var current = ref _palette[GetPaletteIndex(index)];
				if (value == current.Value) return;

				if (--current.RefCount == 0)
					_usedPalettes--;

				var replace = Array.FindIndex(_palette, entry => value == entry.Value);
				if (replace != -1) {
					SetPaletteIndex(index, replace);
					_palette[replace].RefCount += 1;
					return;
				}

				if (current.RefCount == 0) {
					current.Value    = value;
					current.RefCount = 1;
					_usedPalettes++;
					return;
				}
			}

			var newPaletteIndex = NewPaletteEntry();
			_palette![newPaletteIndex] = new(){ Value = value, RefCount = 1 };
			SetPaletteIndex(x, y, z, newPaletteIndex);
			_usedPalettes++;
		}

		private int NewPaletteEntry()
		{
			if (_palette != null) {
				int firstFree = Array.FindIndex(_palette, entry =>
					(entry.Value == null) || (entry.RefCount == 0));
				if (firstFree != -1) return firstFree;
			}

			GrowPalette();
			return NewPaletteEntry();
		}

		private void GrowPalette() {
			if (_palette == null) {
				_data    = new(SIZE);
				_palette = new PaletteEntry[2];
				_usedPalettes  = 1;
				_indicesLength = 1;
				_palette[0] = new(){ Value = Default, RefCount = SIZE };
				return;
			}

			_indicesLength <<= 1;

			var oldIndicesLength = _indicesLength >> 1;
			var newData = new BitArray(SIZE * _indicesLength);
			for (var i = 0; i < SIZE; i++)
			for (var j = 0; j < oldIndicesLength; j++)
				newData.Set(i * _indicesLength + j, _data!.Get(i * oldIndicesLength + j));
			_data = newData;

			var newPalette = new PaletteEntry[1 << _indicesLength];
			Array.Copy(_palette, 0, newPalette, 0, _palette.Length);
			_palette = newPalette;
		}

		// public void FitPalette() {
		// 	if (_usedPalettes > Mathf.NearestPo2(_usedPalettes) / 2) return;

		// 	// decode all indices
		// 	int[] indices = new int[size];
		// 	for(int i = 0; i < indices.length; i++) {
		// 	indices[i] = data.get(i * indicesLength, indicesLength);
		// 	}

		// 	// Create new palette, halfing it in size
		// 	indicesLength = indicesLength >> 1;
		// 	PaletteEntry[] newPalette = new PaletteEntry[2 pow indicesLength];

		// 	// We gotta compress the palette entries!
		// 	int paletteCounter = 0;
		// 	for(int pi = 0; pi < palette.length; pi++, paletteCounter++) {
		// 	PaletteEntry entry = newPalette[paletteCounter] = palette[pi];

		// 	// Re-encode the indices (find and replace; with limit)
		// 	for(int di = 0, fc = 0; di < indices.length && fc < entry.refcount; di++) {
		// 		if(pi == indices[di]) {
		// 		indices[di] = paletteCounter;
		// 		fc += 1;
		// 		}
		// 	}
		// 	}

		// 	// Allocate new BitBuffer
		// 	data = new BitBuffer(size * indicesLength); // the length is in bits, not bytes!

		// 	// Encode the indices
		// 	for(int i = 0; i < indices.length; i++) {
		// 	data.set(i * indicesLength, indicesLength, indices[i]);
		// 	}
		// }


		private int GetPaletteIndex(int x, int y, int z)
			=> GetPaletteIndex(GetIndex(x, y, z));
		private int GetPaletteIndex(int index)
		{
			var paletteIndex = 0;
			for (var i = 0; i < _indicesLength; i++)
				paletteIndex |= (_data!.Get(index + i) ? 1 : 0) << i;
			return paletteIndex;
		}

		private void SetPaletteIndex(int x, int y, int z, int paletteIndex)
			=> SetPaletteIndex(GetIndex(x, y, z), paletteIndex);
		private void SetPaletteIndex(int index, int paletteIndex)
		{
			for (var i = 0; i < _indicesLength; i++)
				_data!.Set(index + i, ((paletteIndex >> i) & 0b1) == 0b1);
		}

		private int GetIndex(int x, int y, int z)
			=> (x | (y << 4) | (z << 8)) * _indicesLength;


		private struct PaletteEntry
		{
			public T? Value { get; set; }
			public int RefCount { get; set; }
		}
	}
}
