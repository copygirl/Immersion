using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Immersion.Utility
{
	public class ImmutableSet<T> : ISet<T>
	{
		private readonly ISet<T> _set;

		public ImmutableSet(IEnumerable<T> collection)
			=> _set = new HashSet<T>(collection);

		public ImmutableSet(params T[] items)
			: this((IEnumerable<T>)items) {  }
		public ImmutableSet(params IEnumerable<T>[] collections)
			: this(collections.SelectMany(x => x)) {  }
		public ImmutableSet(IEnumerable<T> collection, params T[] items)
			: this(collection.Concat(items)) {  }

		// ISet implementation

		bool ISet<T>.Add(T item)
			=> throw new NotSupportedException();

		public bool IsProperSubsetOf(IEnumerable<T> other)
			=> _set.IsProperSubsetOf(other);
		public bool IsProperSupersetOf(IEnumerable<T> other)
			=> _set.IsProperSupersetOf(other);
		public bool IsSubsetOf(IEnumerable<T> other)
			=> _set.IsSubsetOf(other);
		public bool IsSupersetOf(IEnumerable<T> other)
			=> _set.IsSupersetOf(other);
		public bool Overlaps(IEnumerable<T> other)
			=> _set.Overlaps(other);
		public bool SetEquals(IEnumerable<T> other)
			=> _set.SetEquals(other);

		public void ExceptWith(IEnumerable<T> other)
			=> throw new NotSupportedException();
		public void IntersectWith(IEnumerable<T> other)
			=> throw new NotSupportedException();
		public void SymmetricExceptWith(IEnumerable<T> other)
			=> throw new NotSupportedException();
		public void UnionWith(IEnumerable<T> other)
			=> throw new NotSupportedException();

		// ICollection implementation

		public int Count => _set.Count;
		bool ICollection<T>.IsReadOnly => true;

		void ICollection<T>.Add(T item)
			=> throw new NotSupportedException();
		bool ICollection<T>.Remove(T item)
			=> throw new NotSupportedException();
		void ICollection<T>.Clear()
			=> throw new NotSupportedException();

		public bool Contains(T item)
			=> _set.Contains(item);
		void ICollection<T>.CopyTo(T[] array, int arrayIndex)
			=> _set.CopyTo(array, arrayIndex);

		// IEnumerable implementation

		public IEnumerator<T> GetEnumerator()
			=> _set.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator()
			=> _set.GetEnumerator();
	}
}
