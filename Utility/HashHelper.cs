
using System;
using System.Collections;

namespace Immersion.Utility
{
	public static class HashHelper
	{
		// CoreLib => System.Numerics.Hashing.HashHelpers
		// https://github.com/dotnet/coreclr/blob/master/src/System.Private.CoreLib/shared/System/Numerics/Hashing/HashHelpers.cs
		
		public static int Combine(int h1, int h2)
		{
			uint rol5 = ((uint)h1 << 5) | ((uint)h1 >> 27);
			return ((int)rol5 + h1) ^ h2;
		}
		public static int Combine(int h1, int h2, int h3)
			=> Combine(Combine(h1, h2), h3);
		public static int Combine(int h1, int h2, int h3, int h4)
			=> Combine(Combine(Combine(h1, h2), h3), h4);
		
		public static int Combine(params int[] hashCodes)
		{
			if (hashCodes == null) throw new ArgumentNullException(nameof(hashCodes));
			if (hashCodes.Length == 0) throw new ArgumentException(
				"Argument 'hashCodes' is empty", nameof(hashCodes));
			var hash = hashCodes[0];
			for (var i = 1; i < hashCodes.Length; i++)
				hash = Combine(hash, hashCodes[i]);
			return hash;
		}
		
		public static int Combine<A, B>(A a, B b)
			=> Combine(a?.GetHashCode() ?? 0, b?.GetHashCode() ?? 0);
		public static int Combine<A, B, C>(A a, B b, C c)
			=> Combine(a?.GetHashCode() ?? 0, b?.GetHashCode() ?? 0, c?.GetHashCode() ?? 0);
		public static int Combine<A, B, C, D>(A a, B b, C c, D d)
			=> Combine(a?.GetHashCode() ?? 0, b?.GetHashCode() ?? 0, c?.GetHashCode() ?? 0, d?.GetHashCode() ?? 0);
		
		public static int Combine(params object[] objects)
		{
			if (objects == null) throw new ArgumentNullException(nameof(objects));
			if (objects.Length == 0) throw new ArgumentException(
				"Argument 'objects' is empty", nameof(objects));
			var hash = objects[0].GetHashCode();
			for (var i = 1; i < objects.Length; i++)
				hash = Combine(hash, objects[i].GetHashCode());
			return hash;
		}
		
	}
}
