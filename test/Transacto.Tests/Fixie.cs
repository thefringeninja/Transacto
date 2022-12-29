using System.Reflection;
using System.Text;
using AutoFixture;
using AutoFixture.Kernel;
using Fixie;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using SerilogTimings;

namespace Transacto;

public class TestProject : ITestProject {
	private static readonly string[] LifecycleMethods = { "InitializeAsync", "DisposeAsync", "Dispose" };
	private static readonly IEnumerable<object[]> NoParameters = new[] { Array.Empty<object>() };

	static TestProject() {
		Log.Logger = new LoggerConfiguration()
			.Enrich.FromLogContext()
			.WriteTo.Console(
				outputTemplate:
				"{Message:lj}{NewLine}{Exception}",
				theme: AnsiConsoleTheme.Literate)
			.MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
			.CreateLogger();
	}

	public void Configure(TestConfiguration configuration, TestEnvironment environment) {
		configuration.Conventions.Add(new TestDiscovery(), new ParallelExecution());
		configuration.Conventions.Add(new IntegrationTestDiscovery(), new SerialExecution());
	}

	private class TestDiscovery : IDiscovery {
		public IEnumerable<Type> TestClasses(IEnumerable<Type> concreteClasses) => concreteClasses
			.Where(x => x.Name.EndsWith("Tests") && !x.Name.EndsWith("IntegrationTests"))
			.Shuffle();

		public IEnumerable<MethodInfo> TestMethods(IEnumerable<MethodInfo> publicMethods) => publicMethods
			.Where(x => !LifecycleMethods.Contains(x.Name))
			.Shuffle();
	}

	private class IntegrationTestDiscovery : IDiscovery {
		public IEnumerable<Type> TestClasses(IEnumerable<Type> concreteClasses) => concreteClasses
			.Where(x => x.Name.EndsWith("IntegrationTests"));

		public IEnumerable<MethodInfo> TestMethods(IEnumerable<MethodInfo> publicMethods) => publicMethods
			.Where(x => !LifecycleMethods.Contains(x.Name));
	}


	private class ParallelExecution : IExecution {
		public Task Run(TestSuite testSuite) => Task.WhenAll(
			from testClass in testSuite.TestClasses
			from test in testClass.Tests
			from result in RunTest(testClass, test)
			select result);
	}

	private class SerialExecution : IExecution {
		public async Task Run(TestSuite testSuite) {
			foreach (var testClass in testSuite.TestClasses) {
				foreach (var test in testClass.Tests) {
					foreach (var result in RunTest(testClass, test)) {
						await result;
					}
				}
			}
		}
	}

	private static IEnumerable<Task> RunTest(TestClass testClass, Test test) =>
		from parameters in UsingInputAttributes(test)
		let instance = testClass.Construct(GetConstructorParameters(testClass))
		select RunSingleTest(testClass, test, instance, parameters);

	private static async Task RunSingleTest(TestClass testClass, Test test, object instance,
		object[] parameters) {
		using var testOp = Operation.At(LogEventLevel.Information, LogEventLevel.Error)
			.Begin("{TestName}({Parameters})", test.Name, parameters);

		try {
			await TryLifecycleMethod(testClass, instance, "InitializeAsync");
			var result = await test.Run(instance, parameters);
			if (result is Failed failed) {
				testOp.Abandon(failed.Reason);
			}

			testOp.Complete();
		} finally {
			await TryLifecycleMethod(testClass, instance, "DisposeAsync");
		}
	}

	private static object[] GetConstructorParameters(TestClass testClass) => testClass.Type.GetInterfaces()
		.Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IClassFixture<>))
		.Select(@interface => Activator.CreateInstance(@interface.GetGenericArguments()[0])!)
		.ToArray();


	private static Task TryLifecycleMethod(TestClass testClass, object instance, string name) =>
		testClass.Type.GetMethod(name)?.Call(instance) ?? Task.CompletedTask;

	private static IEnumerable<object[]> UsingInputAttributes(Test test)
		=> test.HasParameters
			? test.GetAll<DataAttribute>().SelectMany(input => input.GetParameters(test))
			: NoParameters;
}

internal interface IClassFixture<T> where T : new() {
}

internal abstract class DataAttribute : Attribute {
	public abstract IEnumerable<object[]> GetParameters(Test test);
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
internal class InlineDataAttribute : DataAttribute {
	public IEnumerable<object[]> Parameters { get; }

	public InlineDataAttribute(params object[] parameters) => Parameters = new[] { parameters };

	public override IEnumerable<object[]> GetParameters(Test test) => Parameters;
}

[AttributeUsage(AttributeTargets.Method)]
internal class ClassDataAttribute : DataAttribute {
	public IEnumerable<object[]> Parameters { get; }

	public ClassDataAttribute(Type classType) {
		if (!typeof(IEnumerable<object[]>).IsAssignableFrom(classType)) {
			throw new ArgumentException($"Expected {classType.FullName} to implement IEnumerable<object[]>.",
				nameof(classType));
		}

		if (classType.GetConstructor(Array.Empty<Type>()) is null) {
			throw new ArgumentException($"Expected {classType.FullName} to have an empty parameterless constructor.",
				nameof(classType));
		}

		Parameters = (IEnumerable<object[]>)Activator.CreateInstance(classType)!;
	}

	public override IEnumerable<object[]> GetParameters(Test test) => Parameters;
}

[AttributeUsage(AttributeTargets.Method)]
internal class AutoFixtureDataAttribute : DataAttribute {
	private readonly int _count;
	private readonly Fixture _fixture;

	public AutoFixtureDataAttribute(int count = 3) {
		_count = count;
		_fixture = new ScenarioFixture();
	}

	public override IEnumerable<object[]> GetParameters(Test test) {
		for (var i = 0; i < _count; i++) {
			yield return Array.ConvertAll(test.Parameters,
				parameter => _fixture.Create(parameter.ParameterType, new SpecimenContext(_fixture)));
		}
	}
}

[AttributeUsage(AttributeTargets.Method)]
internal class MemberDataAttribute : DataAttribute {
	public string Name { get; }

	public MemberDataAttribute(string name) {
		Name = name;
	}

	public override IEnumerable<object[]> GetParameters(Test test) =>
		(test.Method.DeclaringType?.GetMethod(Name,
				BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
			?.Invoke(null, null) as IEnumerable<object[]>)!;
}

internal class SerilogTextWriter : TextWriter {
	private readonly ILogger _logger;
	private readonly StringBuilder _builder;

	public override Encoding Encoding { get; } = Encoding.UTF8;

	public SerilogTextWriter(ILogger logger) {
		_logger = logger;
		_builder = new StringBuilder();
	}

	public override void Write(char value) {
		_builder.Append(value);

		if (!Environment.NewLine.Contains(value) || !EndsWithNewLine()) {
			return;
		}

		_logger.Information(_builder.ToString().Trim());
		_builder.Clear();
	}

	private bool EndsWithNewLine() {
		for (var i = Environment.NewLine.Length; i > 0; i--) {
			if (_builder[^i] != Environment.NewLine[^i]) {
				return false;
			}
		}

		return true;
	}
}
