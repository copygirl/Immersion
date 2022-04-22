using System;
using System.Linq;
using System.Reflection;
using Godot;
using Immersion.Tests;

// TODO: Replace with proper unit test framework when possible.
//       At this time, attempting to set up xunit wasn't working.
public class TestRunner : Node
{
	public override void _Ready()
	{
		GD.Print("Running tests in TestRunner scene ...");

		const BindingFlags FLAGS =
			BindingFlags.Public | BindingFlags.NonPublic |
			BindingFlags.Static | BindingFlags.Instance;

		var testMethodCandidates = typeof(TestRunner).Assembly.GetTypes()
			.SelectMany(type => type.GetMethods(FLAGS))
			.Where(method => method.GetCustomAttribute<Test>() != null)
			.ToArray();

		var total  = testMethodCandidates.Length;
		var passed = 0;
		var failed = 0;

		GD.Print($"Found {total} test methods to execute.");

		foreach (var method in testMethodCandidates) {
			try {
				if (!method.IsStatic) throw new Exception("Must be static");
				if (method.ReturnType != typeof(void)) throw new Exception("Must have void return type");
				if (method.GetParameters().Length > 0) throw new Exception("Must have 0 parameters");
				method.Invoke(null, Array.Empty<object>());
				passed++;
			} catch (Exception ex) {
				if (ex is TargetInvocationException ex2) ex = ex2.InnerException;
				var fullName = $"{method.DeclaringType.FullName}.{method.Name}";
				GD.PrintErr($"Test method '{fullName}' failed with:\n" +
				            $"{ex.Message}\n" +
							$"{ex.StackTrace.Split('\n')[1]}");
				failed++;
			}
		}

		GD.Print("Testing has completed!");
		GD.Print($"Total: {total} | Passed: {passed} | Failed: {failed}");

		GetTree().Quit();
	}
}
