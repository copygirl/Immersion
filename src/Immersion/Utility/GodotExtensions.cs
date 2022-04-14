using Godot;

namespace Immersion.Utility
{
	public static class GodotExtensions
	{
		public static void Deconstruct(this Vector2 vec, out float x, out float y)
			=> (x, y) = (vec.x, vec.y);
		public static void Deconstruct(this Vector3 vec, out float x, out float y, out float z)
			=> (x, y, z) = (vec.x, vec.y, vec.z);
	}
}
