using System;
using System.Collections.Generic;

namespace Immersion.Tests
{
	public static class Assert
	{
		public static void Equal<T>(T expected, T value)
		{
			if (!EqualityComparer<T>.Default.Equals(value, expected))
				throw new AssertException("Value does not equal expected value!\n" +
					$"Value: {value}\n" +
					$"Expected: {expected}");
		}

		public static void Equal(float expected, float value, float margin = 0.0001F)
			=> Equal((double)expected, (double)value, (double)margin);
		public static void Equal(double expected, double value, double margin = 0.000001)
		{
			if (Math.Abs(value - expected) > margin)
				throw new AssertException("Value does not equal expected value!\n" +
					$"Value: {value}\n" +
					$"Expected: {expected}\n" +
					$"Error margin: {margin}");
		}

		public static void True(bool value)
			{ if (!value) throw new AssertException("Value does not equal true!"); }
		public static void False(bool value)
			{ if (value) throw new AssertException("Value does not equal false!"); }

		public static void Throws<TException>(Action action)
			where TException : Exception
		{
			try {
				action();
				throw new AssertException("Did not throw expected exception!\n" +
					$"Expected: {typeof(TException)}");
			} catch (TException) {
				// All good!
			}
		}
		public static void Throws<TException>(string expectedMessage, Action action)
			where TException : Exception
		{
			try {
				action();
				throw new AssertException("Did not throw expected exception!\n" +
					$"Expected: {typeof(TException)}");
			} catch (TException ex) {
				if (ex.Message != expectedMessage)
					throw new AssertException("Exception does not have expected message!\n" +
						$"Message: {ex.Message}\n" +
						$"Expected: {expectedMessage}");
				// All good!
			}
		}
	}

	public class AssertException : Exception
	{
		public AssertException(string message)
			: base("Assertion failed: " + message) {  }
	}
}
