using System;
using System.Collections.Generic;

namespace Immersion.Tests
{
	public static class Assert
	{
		public static void Equal<T>(T value, T expected)
		{
			if (!EqualityComparer<T>.Default.Equals(value, expected)) throw new AssertException(
				$"Value does not equal expected value!\nValue: {value}\nExpected: {expected}");
		}
	}

	public class AssertException : Exception
	{
		public AssertException(string message)
			: base("Assertion failed: " + message) {  }
	}
}
