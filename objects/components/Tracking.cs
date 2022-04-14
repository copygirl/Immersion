using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public class Tracking : Node
{
	[Signal] public delegate void OnStartedTracking(Trackable obj);
	[Signal] public delegate void OnStoppedTracking(Trackable obj);

	public HashSet<Trackable> TrackedObjects { get; } = new();


	public void OnEntered(Area area)
	{
		if (area is not Trackable trackable) throw new Exception("Non-trackable entered tracking area: " + area);
		if (!TrackedObjects.Add(trackable)) throw new Exception("Newly tracked object was already tracked:" + area);
		trackable.TrackedBy.Add(this);
		EmitSignal(nameof(OnStartedTracking), trackable);
		trackable.EmitSignal(nameof(Trackable.OnStartedTracking), this);
		GD.Print(GetParent(), " started tracking " + trackable.GetParent());
	}

	public void OnExited(Area area)
	{
		if (area is not Trackable trackable) throw new Exception("Non-trackable exited tracking area: " + area);
		if (!TrackedObjects.Remove(trackable)) throw new Exception("Untracked object wasn't tracked before:" + area);
		trackable.TrackedBy.Remove(this);
		EmitSignal(nameof(OnStoppedTracking), trackable);
		trackable.EmitSignal(nameof(Trackable.OnStoppedTracking), this);
		GD.Print(GetParent(), " stopped tracking " + trackable.GetParent());
	}

	public void OnTreeExited()
	{
		var tracked = TrackedObjects.ToArray();
		TrackedObjects.Clear();
		foreach (var trackable in tracked) {
			trackable.TrackedBy.Remove(this);
			EmitSignal(nameof(OnStoppedTracking), trackable);
			trackable.EmitSignal(nameof(Trackable.OnStoppedTracking), this);
		}
	}
}
