using System.Collections.Generic;
using Godot;
using Immersion.Utility;

[Tool]
public class Trackable : Area
{
	[Signal] public delegate void OnStartedTracking(Tracking trackedBy);
	[Signal] public delegate void OnStoppedTracking(Tracking trackedBy);

	[Export] public bool TrackAutomatically {
		get => (CollisionLayer & (uint)CollisionLayers.Tracking) != 0;
		set => CollisionLayer = value ? (uint)CollisionLayers.Tracking : 0;
	}
	[Export] public Shape Shape {
		get => _collisionShape.Shape;
		set => _collisionShape.Shape = value;
	}


	private readonly CollisionShape _collisionShape = new();

	public HashSet<Tracking> TrackedBy { get; } = new();

	public Trackable()
	{
		AddChild(_collisionShape);
		TrackAutomatically = true;
	}
}
