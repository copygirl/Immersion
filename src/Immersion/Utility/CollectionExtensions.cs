using System;
using System.Collections.Generic;

namespace Immersion.Utility
{
	public static class CollectionExtensions
	{
		public static void Deconstruct<TKey, TValue>(
				this KeyValuePair<TKey, TValue> kvp, out TKey key, out TValue value)
			=> (key, value) = (kvp.Key, kvp.Value);


		public static TValue GetOrElse<TKey, TValue>(
				this IDictionary<TKey, TValue> self, TKey key, TValue @default)
			=> self.TryGetValue(key, out var value) ? value : @default;
		public static TValue GetOrElse<TKey, TValue>(
				this IDictionary<TKey, TValue> self, TKey key, Func<TValue> defaultFunc)
			=> self.TryGetValue(key, out var value) ? value : defaultFunc();


		public static TValue? GetOrNull<TKey, TValue>(
			this IDictionary<TKey, TValue> self, TKey key)
				where TValue : class
			=> self.TryGetValue(key, out var value) ? value : null;

		public static TValue GetOrDefault<TKey, TValue>(
			this IDictionary<TKey, TValue> self, TKey key)
				where TValue : struct
			=> self.TryGetValue(key, out var value) ? value : default;

		public static TValue? GetNullable<TKey, TValue>(
			this IDictionary<TKey, TValue> self, TKey key)
				where TValue : struct
			=> self.TryGetValue(key, out var value) ? value : null;


		public static TValue GetOrAdd<TKey, TValue>(
			this IDictionary<TKey, TValue> self, TKey key, TValue @default)
		{
			if (!self.TryGetValue(key, out var value))
				self.Add(key, value = @default);
			return value;
		}

		public static TValue GetOrAdd<TKey, TValue>(
			this IDictionary<TKey, TValue> self, TKey key, Func<TValue> defaultFunc)
		{
			if (!self.TryGetValue(key, out var value))
				self.Add(key, value = defaultFunc());
			return value;
		}


		public static TValue GetOrThrow<TKey, TValue>(
			this IDictionary<TKey, TValue> self, TKey key, Func<Exception> errorFunc)
		{
			if (!self.TryGetValue(key, out var value))
				throw errorFunc();
			return value;
		}


		public static T FirstOrElse<T>(this IEnumerable<T> self, T @default)
		{
			var enumerator = self.GetEnumerator();
			return enumerator.MoveNext() ? enumerator.Current : @default;
		}
		public static T FirstOrElse<T>(this IEnumerable<T> self, Func<T> defaultFunc)
		{
			var enumerator = self.GetEnumerator();
			return enumerator.MoveNext() ? enumerator.Current : defaultFunc();
		}

		public static T? FirstOrNull<T>(this IEnumerable<T> self)
			where T : class
		{
			var enumerator = self.GetEnumerator();
			return enumerator.MoveNext() ? enumerator.Current : null;
		}
		public static T? FirstNullable<T>(this IEnumerable<T> self)
			where T : struct
		{
			var enumerator = self.GetEnumerator();
			return enumerator.MoveNext() ? enumerator.Current : null;
		}
	}

	public class ReverseComparer<T> : IComparer<T>
	{
		private readonly IComparer<T> _original;

		public ReverseComparer(IComparer<T> comparer)
			=> _original = comparer;
		public ReverseComparer()
			: this(Comparer<T>.Default) {  }

		public int Compare(T x, T y)
			=> -_original.Compare(x, y);
	}
}
