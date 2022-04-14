using System;

namespace Immersion.Utility
{
	[Flags]
	public enum CollisionLayers : uint
	{
		Tracking = 0b00000010,
		World    = 0b00000100,
		Players  = 0b00001000,
		Entities = 0b00010000,
		Items    = 0b00100000,
	}
}
