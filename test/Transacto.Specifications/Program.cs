using System.CommandLine;
using System.Globalization;
using AutoFixture.Kernel;
using Bogus;
using Inflector;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;
using Serilog.Sinks.SystemConsole.Themes;
using Transacto;
using Transacto.Specifications;

Inflector.Inflector.SetDefaultCultureFunc = () => CultureInfo.CurrentCulture;

Option<string> formatOption = new("--format") {
};

var rootCommand = new RootCommand() {
	formatOption
};

var parseResult = rootCommand.Parse(args);

if (parseResult.Errors.Count > 0) {
	return -1;
}

ISpecificationWriter writer = parseResult.GetValue(formatOption) switch {
	"html" => new HtmlSpecificationWriter(Console.Out),
	"text" or null => new LoggerSpecificationWriter(
		new SerilogLoggerFactory(
				new LoggerConfiguration()
					.WriteTo.Console(outputTemplate: "{Message:lj}{NewLine}{Exception}",
						theme: AnsiConsoleTheme.Literate)
					.CreateLogger())
			.CreateLogger<Program>()),
	_ => null!
};

var fixture = new ScenarioFixture(new Randomizer(1776));
var specificationGroupTypes = GetSpecificationGroupTypes<Program>();
foreach (var specificationGroupType in specificationGroupTypes) {
	using var _ = writer.BeginSpecificationGroup(specificationGroupType.Name.Underscore().Humanize());
	foreach (var spec in GetSpecificationTypes(specificationGroupType)) {
		var constructor = spec.GetConstructors()[0];

		var parameters = Array.ConvertAll(
			constructor.GetParameters(),
			pi => fixture.Create(pi.ParameterType, new SpecimenContext(fixture)));

		var specInstance = Activator.CreateInstance(spec, parameters);

		var specificationName = spec.Name.Underscore().Humanize();

		switch (specInstance) {
			case ExceptionSpecification ex:
				writer.WriteSpecification(specificationName, ex);
				break;
			case EventSpecification ev:
				writer.WriteSpecification(specificationName, ev);
				break;
		}
	}

}

await writer.FlushAsync();

return 0;

static IEnumerable<Type> GetSpecificationGroupTypes<T>() =>
	from containerType in typeof(T).Assembly.GetTypes()
	where containerType.DeclaringType is null &&
	      containerType.Namespace == typeof(GeneralLedgerSpecifications).Namespace
	orderby containerType.Name
	select containerType;

static IEnumerable<Type> GetSpecificationTypes(Type containerType) =>
	from type in containerType.GetNestedTypes()
	where type.IsAssignableTo(typeof(EventSpecification)) ||
	      type.IsAssignableTo(typeof(ExceptionSpecification))
	orderby type.Name
	select type;
