using System.Globalization;
using AutoFixture.Kernel;
using Inflector;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;
using Serilog.Sinks.SystemConsole.Themes;
using Transacto;
using Transacto.Testing;

Inflector.Inflector.SetDefaultCultureFunc = () => CultureInfo.CurrentCulture;

var logger = new SerilogLoggerFactory(
		new LoggerConfiguration()
			.WriteTo.Console(outputTemplate: "{Message:lj}{NewLine}{Exception}", theme: AnsiConsoleTheme.Literate)
			.CreateLogger())
	.CreateLogger(typeof(Program));

var writer = new TestSpecificationLogger(logger);

var fixture = new ScenarioFixture();

var specs = GetSpecificationTypes<Program>();

foreach (var spec in specs) {
	var constructor = spec.GetConstructors()[0];
	
	var parameters = Array.ConvertAll(
		constructor.GetParameters(),
		pi => fixture.Create(pi.ParameterType, new SpecimenContext(fixture)));

	var specInstance = Activator.CreateInstance(spec, parameters);
	
	Console.WriteLine($"\e[1m{spec.Name.Underscore().Humanize()}\e[0m");
	switch (specInstance) {
		case ExceptionSpecification ex:
			writer.Write(ex);
			break;
		case EventSpecification ev:
			writer.Write(ev);
			break;
	}
	Console.WriteLine();
}

return 0;

static IEnumerable<Type> GetSpecificationTypes<T>() {
	foreach (var containerType in typeof(T).Assembly.GetTypes().Where(type => type.DeclaringType is null)) {
		foreach (var type in containerType.GetNestedTypes()) {
			if (type.IsAssignableTo(typeof(EventSpecification)) ||
			    type.IsAssignableTo(typeof(ExceptionSpecification))) {
				yield return type;
			}
		}
	}
}
