using System.Globalization;
using System.Reflection;
using AutoFixture;
using AutoFixture.Kernel;
using Fixie;
using Inflector;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Serilog.Sinks.SystemConsole.Themes;
using SerilogTimings;
using Transacto.Testing;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Transacto;

public class TestProject : ITestProject {
	private static readonly string[] LifecycleMethods = ["InitializeAsync", "DisposeAsync", "Dispose"];
	private static readonly IEnumerable<object[]> NoParameters = [[]];

	public void Configure(TestConfiguration configuration, TestEnvironment environment) {
		if (environment.CustomArguments.Contains("--print")) {
			configuration.Conventions.Add(new SpecificationDiscovery(), new SpecificationDiscovery.Print(environment));
		} else {
			configuration.Conventions.Add(new TestDiscovery(), new ParallelExecution());
			configuration.Conventions.Add(new IntegrationTestDiscovery(), new SerialExecution());
		}
	}
	
	private class SpecificationDiscovery : IDiscovery {
		public SpecificationDiscovery() {
			Inflector.Inflector.SetDefaultCultureFunc = () => CultureInfo.GetCultureInfo("en");
		}
		public IEnumerable<Type> TestClasses(IEnumerable<Type> concreteClasses) {
			foreach (var type in concreteClasses) {
				if (typeof(EventSpecification).IsAssignableFrom(type) || typeof(ExceptionSpecification).IsAssignableFrom(type)) {
					yield return type;
				}
			}
		}

		public IEnumerable<MethodInfo> TestMethods(IEnumerable<MethodInfo> publicMethods) => publicMethods;

		public class Print(TestEnvironment environment) : IExecution {
			private readonly ILogger _logger = new SerilogLoggerFactory(
					new LoggerConfiguration()
						.WriteTo.Console(outputTemplate: "{Message:lj}{NewLine}{Exception}", theme: AnsiConsoleTheme.Literate)
						.CreateLogger())
				.CreateLogger(typeof(TestProject));
			public Task Run(TestSuite testSuite) {
				var fixture = new ScenarioFixture();
				var writer = new TestSpecificationLogger(_logger);
				for (var i = 0; i < testSuite.TestClasses.Count; i++) {
					var parameters = Array.ConvertAll(
						testSuite.TestClasses[i].Type.GetConstructors()[0].GetParameters(),
						pi => fixture.Create(pi.ParameterType, new SpecimenContext(fixture)));

					var spec = testSuite.TestClasses[i].Construct(parameters);

					_logger.LogInformation(testSuite.TestClasses[i].Type.Name.Titleize());
					switch (spec) {
						case ExceptionSpecification ex:
							writer.Write(ex);
							break;
						case EventSpecification ev:
							writer.Write(ev);
							break;
					}
					environment.Console.WriteLine();
				}

				return Task.CompletedTask;
			}
		}
	}

	private class TestDiscovery : IDiscovery {
		public IEnumerable<Type> TestClasses(IEnumerable<Type> concreteClasses) => concreteClasses
			.Where(x => x.Name.EndsWith("Tests") && !x.Name.EndsWith("IntegrationTests"))
		//.Shuffle()
		;

		public IEnumerable<MethodInfo> TestMethods(IEnumerable<MethodInfo> publicMethods) => publicMethods
			.Where(x => !LifecycleMethods.Contains(x.Name))
		//.Shuffle()
		;
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
internal class InlineDataAttribute(params object[] parameters) : DataAttribute {
	public IEnumerable<object[]> Parameters { get; } = [parameters];

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

		if (classType.GetConstructor([]) is null) {
			throw new ArgumentException($"Expected {classType.FullName} to have an empty parameterless constructor.",
				nameof(classType));
		}

		Parameters = (IEnumerable<object[]>)Activator.CreateInstance(classType)!;
	}

	public override IEnumerable<object[]> GetParameters(Test test) => Parameters;
}

[AttributeUsage(AttributeTargets.Method)]
internal class AutoFixtureDataAttribute(int count = 3) : DataAttribute {
	private readonly Fixture _fixture = new ScenarioFixture();

	public override IEnumerable<object[]> GetParameters(Test test) {
		for (var i = 0; i < count; i++) {
			yield return Array.ConvertAll(test.Parameters,
				parameter => _fixture.Create(parameter.ParameterType, new SpecimenContext(_fixture)));
		}
	}
}

[AttributeUsage(AttributeTargets.Method)]
internal class MemberDataAttribute(string name) : DataAttribute {
	public string Name { get; } = name;

	public override IEnumerable<object[]> GetParameters(Test test) =>
		(test.Method.DeclaringType?.GetMethod(Name,
				BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
			?.Invoke(null, null) as IEnumerable<object[]>)!;
}
