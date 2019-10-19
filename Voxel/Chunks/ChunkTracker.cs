using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Immersion.Utility;

namespace Immersion.Voxel.Chunks
{
	public class ChunkTracker
	{
		private readonly Dictionary<Spatial, TrackedSpatialData> _spatials
			= new Dictionary<Spatial, TrackedSpatialData>();
		private readonly Dictionary<ChunkPos, TrackedChunkData> _chunks
			= new Dictionary<ChunkPos, TrackedChunkData>();
		private readonly SortedDictionary<int, HashSet<ChunkPos>> _toSimulate
			= new SortedDictionary<int, HashSet<ChunkPos>>(new ReverseComparer<int>());
		
		public ChunkManager Chunks { get; }
		
		public IEnumerable<ChunkPos> SimulationRequestedChunks
			=> _toSimulate.SelectMany(kvp => kvp.Value);
		
		
		public ChunkTracker(ChunkManager chunks)
			=> Chunks = chunks;
		
		
		public void StartTracking(Spatial tracked, int simDistance)
			=> StartTracking(tracked, simDistance, simDistance + 4);
		public void StartTracking(Spatial tracked, int simDistance, int keepDistance)
		{
			if (_spatials.ContainsKey(tracked)) throw new InvalidOperationException(
				$"The Spatial node '{tracked}' is already being tracked");
			var data = new TrackedSpatialData(tracked, simDistance, keepDistance);
			_spatials.Add(tracked, data);
		}
		
		public void StopTracking(Spatial tracked)
		{
			var data = _spatials.GetOrThrow(tracked, () => new ArgumentException(
				$"'{nameof(tracked)}' (={tracked}) is not being tracked", nameof(tracked)));
			_spatials.Remove(tracked);
			foreach (var chunk in data.Chunks)
				Untrack(data, chunk);
		}
		
		public void Update()
		{
			foreach (var data in _spatials.Values)
				data.Update(Track, Untrack);
		}
		
		public void MarkChunkReady(ChunkPos pos)
		{
			foreach (var (_, chunks) in _toSimulate)
				if (chunks.Remove(pos)) return;
		}
		
		
		private void Track(TrackedSpatialData data, ChunkPos pos, float distance)
		{
			var chunk  = _chunks.GetOrAdd(pos, () => new TrackedChunkData());
			var weight = Mathf.FloorToInt((data.SimulatingDistance - distance) * 100);

			if (chunk.TrackedBy.Count > 0) {
				chunk.TrackedBy[data] = weight;
				if ((chunk.MaxTrackedBy == data) ? (weight != chunk.MaxWeight)
				                                 : (weight > chunk.MaxWeight)) {
					var prevWeight = chunk.MaxWeight;
					chunk.RecalculateMax();
					OnChunkWeightChanged(pos, prevWeight, chunk.MaxWeight);
				}
			} else {
				chunk.TrackedBy.Add(data, weight);
				chunk.MaxTrackedBy = data;
				chunk.MaxWeight    = weight;
				OnChunkStartTracking(pos, weight);
			}
		}
		
		private void Untrack(TrackedSpatialData data, ChunkPos pos)
		{
			var chunk = _chunks.GetOrThrow(pos, () => throw new InvalidOperationException());
			chunk.TrackedBy.Remove(data);
			if (chunk.MaxTrackedBy != data) return;
			
			if (chunk.TrackedBy.Count > 0) {
				var prevWeight = chunk.MaxWeight;
				chunk.RecalculateMax();
				OnChunkWeightChanged(pos, prevWeight, chunk.MaxWeight);
			} else {
				OnChunkStopTracking(pos, chunk.MaxWeight);
				_chunks.Remove(pos);
				Chunks.TryRemove(pos);
			}
		}
		
		
		private void OnChunkStartTracking(ChunkPos pos, int weight)
		{
			if ((weight >= 0) && (Chunks[pos]?.State != ChunkState.Ready))
				_toSimulate.GetOrAdd(weight, () => new HashSet<ChunkPos>()).Add(pos);
		}
		
		private void OnChunkWeightChanged(ChunkPos pos, int oldWeight, int newWeight)
		{
			if (oldWeight == newWeight) throw new ArgumentException(
				$"{nameof(oldWeight)} == {nameof(newWeight)} (={oldWeight:F2})");
			OnChunkStopTracking(pos, oldWeight);
			OnChunkStartTracking(pos, newWeight);
		}
		
		private void OnChunkStopTracking(ChunkPos pos, int weight)
		{
			if (weight >= 0) {
				var chunks = _toSimulate.GetOrNull(weight);
				if (chunks != null) {
					chunks.Remove(pos);
					if (chunks.Count == 0)
						_toSimulate.Remove(weight);
				}
			}
		}
		
		
		/// <summary>
		/// Contains information about a tracked Spatial node, which is able
		/// to track a number of chunks a specified distance away from its
		/// position.
		/// </summary>
		private class TrackedSpatialData
		{
			public Spatial Tracked { get; }
			/// <summary> Distance in chunks this Spatial node wants to be simulated. </summary>
			public int SimulatingDistance { get; }
			/// <summary> Distance in chunks to keep chunks loaded around this Spatial node. </summary>
			public int KeepLoadedDistance { get; }
			/// <summary> Helper lookup of ChunkPos -> distance centered on (0, 0, 0). </summary>
			public DistanceLookup DistanceLookup { get; }
			/// <summary> Chunks that may be tracked by this Spatial node. </summary>
			public HashSet<ChunkPos> Chunks { get; } = new HashSet<ChunkPos>();
			
			public ChunkPos? LastChunkPos { get; private set; }
			
			public TrackedSpatialData(Spatial tracked, int simDistance, int keepDistance)
			{
				Tracked = tracked;
				SimulatingDistance = simDistance;
				KeepLoadedDistance = keepDistance;
				DistanceLookup = GetOrCreateDistanceLookup(keepDistance);
			}
			
			public bool Update(Action<TrackedSpatialData, ChunkPos, float> onChanged,
			                   Action<TrackedSpatialData, ChunkPos> onRemoved)
			{
				var currentChunkPos = Tracked.Transform.origin.ToChunkPos();
				if (currentChunkPos == LastChunkPos) return false;
				
				var d = currentChunkPos - (LastChunkPos ?? currentChunkPos);
				foreach (var (p, distance) in DistanceLookup) {
					var pos = currentChunkPos + p;
					Chunks.Add(pos);
					onChanged(this, pos, distance);
					
					if (LastChunkPos != null)
					if (!DistanceLookup.ContainsKey(p - d)) {
						var oldPos = LastChunkPos.Value + p;
						Chunks.Remove(oldPos);
						onRemoved(this, oldPos);
					}
				}
				
				LastChunkPos = currentChunkPos;
				return true;
			}
		}
		
		/// <summary>
		/// Contains information about which Spatial nodes are currently
		/// tracking a chunk and how far away each is.
		/// </summary>
		private class TrackedChunkData
		{
			public Dictionary<TrackedSpatialData, int> TrackedBy { get; }
				= new Dictionary<TrackedSpatialData, int>();
			
			public TrackedSpatialData? MaxTrackedBy { get; set; } = null;
			public int MaxWeight { get; set; } = int.MinValue;
			
			public bool RecalculateMax()
			{
				var oldWeight = MaxWeight;
				MaxWeight     = int.MinValue;
				foreach (var (data, weight) in TrackedBy) {
					if (weight >= MaxWeight) {
						MaxTrackedBy = data;
						MaxWeight    = weight;
						if (weight == oldWeight) return false;
					}
				}
				return true;
			}
		}
		
		
		private static readonly Dictionary<int, DistanceLookup> _distanceLookupLookup
			= new Dictionary<int, DistanceLookup>();
		
		private static DistanceLookup GetOrCreateDistanceLookup(int keepDistance)
			=> _distanceLookupLookup.GetOrAdd(keepDistance,
				() => new DistanceLookup(keepDistance));
		
		private class DistanceLookup
			: Dictionary<ChunkPos, float>
		{
			public DistanceLookup(int keepDistance)
			{
				var maxDistanceSqr = Mathf.Pow(keepDistance + 0.5F, 2);
				for (var x = -keepDistance; x <= keepDistance; x++)
				for (var y = -keepDistance; y <= keepDistance; y++)
				for (var z = -keepDistance; z <= keepDistance; z++) {
					var distanceSqr = (x * x) + (y * y) + (z * z);
					if (distanceSqr <= maxDistanceSqr)
						Add((x, y, z), Mathf.Sqrt(distanceSqr));
				}
			}
		}
	}
}
