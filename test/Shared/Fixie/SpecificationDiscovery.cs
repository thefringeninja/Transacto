using System.Globalization;
using System.Reflection;
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

internal class SpecificationDiscovery : IDiscovery {
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


}
