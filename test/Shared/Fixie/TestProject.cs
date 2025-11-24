using System.Reflection;
using Fixie;
using Serilog.Events;
using SerilogTimings;
using Transacto.Testing;

namespace Transacto;

partial class TestProject : ITestProject {
	private static readonly string[] LifecycleMethods = ["InitializeAsync", "DisposeAsync", "Dispose"];
	private static readonly IEnumerable<object[]> NoParameters = [[]];

	public void Configure(TestConfiguration configuration, TestEnvironment environment) {
		configuration.Conventions.Add(new TestDiscovery(), new Execution(environment));
	}

	private partial class TestDiscovery(
		Func<IEnumerable<Type>, IEnumerable<Type>> getTestClasses,
		Func<IEnumerable<MethodInfo>, IEnumerable<MethodInfo>> getTestMethods) : IDiscovery {
		public IEnumerable<Type> TestClasses(IEnumerable<Type> concreteClasses) => getTestClasses(concreteClasses);

		public IEnumerable<MethodInfo> TestMethods(IEnumerable<MethodInfo> publicMethods) =>
			getTestMethods(publicMethods);
	}

#pragma warning disable CS9113 // Parameter is unread.
	private partial class Execution(TestEnvironment environment) : IExecution {
		private IEnumerable<Task> AllTests(TestSuite testSuite)  => 
			from testClass in testSuite.TestClasses
			from test in testClass.Tests
			from result in RunTest(testClass, test)
			select result;
		
		private IEnumerable<Task> RunTest(TestClass testClass, Test test) =>
			from parameters in UsingInputAttributes(test)
			let instance = testClass.Construct(GetConstructorParameters(testClass))
			select RunSingleTest(testClass, test, instance, parameters);
		
		private object?[] GetConstructorParameters(TestClass testClass) {
			var fixtureTypes = testClass.Type.GetInterfaces()
				.Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IClassFixture<>))
				.Select(@interface => @interface.GetGenericArguments()[0]);

			return Array.ConvertAll(testClass.Type.GetConstructors().Single()
				.GetParameters(), parameter => {
				if (fixtureTypes.Contains(parameter.ParameterType)) {
					return Activator.CreateInstance(parameter.ParameterType);
				}

				if (parameter.ParameterType == typeof(TextWriter)) {
					return environment.Console;
				}

				return null;
			});
		}

	}

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


	private static Task TryLifecycleMethod(TestClass testClass, object instance, string name) =>
		testClass.Type.GetMethod(name)?.Call(instance) ?? Task.CompletedTask;

	private static IEnumerable<object[]> UsingInputAttributes(Test test)
		=> test.HasParameters
			? test.GetAll<DataAttribute>().SelectMany(input => input.GetParameters(test))
			: NoParameters;
}
