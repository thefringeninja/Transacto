using System.Globalization;
using AutoFixture.Kernel;
using Fixie;
using Inflector;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;
using Serilog.Sinks.SystemConsole.Themes;
using Transacto.Testing;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Transacto;

partial class TestProject {
	partial class TestDiscovery {
		public TestDiscovery() : this(
			GetTestClasses,
			publicMethods => publicMethods) {
			Inflector.Inflector.SetDefaultCultureFunc = () => CultureInfo.GetCultureInfo("en");
		}

		private static IEnumerable<Type> GetTestClasses(IEnumerable<Type> concreteClasses) {
			foreach (var type in concreteClasses) {
				if (typeof(EventSpecification).IsAssignableFrom(type) ||
				    typeof(ExceptionSpecification).IsAssignableFrom(type)) {
					yield return type;
				}
			}
		}
	}

	partial class Execution {
		private readonly ILogger _logger = new SerilogLoggerFactory(
				new LoggerConfiguration()
					.WriteTo.Console(outputTemplate: "{Message:lj}{NewLine}{Exception}",
						theme: AnsiConsoleTheme.Literate)
					.CreateLogger())
			.CreateLogger<TestProject>();

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
