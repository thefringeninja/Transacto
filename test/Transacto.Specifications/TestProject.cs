using System.Globalization;
using AutoFixture;
using AutoFixture.Kernel;
using Fixie;
using Inflector;
using Transacto.Domain;
using Transacto.Testing;

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
					yield return typeof(Lol<>).MakeGenericType(type);
				}
			}
		}
	}

	private class Lol<T> {
		public void Specification(){}
	}

	partial class Execution {
		private readonly TestSpecificationTextWriter _writer = new(environment.Console);
		public async Task Run(TestSuite testSuite) {
			var fixture = new ScenarioFixture();
			fixture.Customize<ChartOfAccountsSettings>(composer =>
				composer.FromFactory(() => ChartOfAccountsSettings.Default));
			for (var i = 0; i < testSuite.TestClasses.Count; i++) {
				var testClass = testSuite.TestClasses[i];
				var specificationType = testClass.Type.GenericTypeArguments[0];
				var constructorInfo = specificationType.GetConstructors()[0];
				var parameters = Array.ConvertAll(
					constructorInfo.GetParameters(),
					pi => fixture.Create(pi.ParameterType, new SpecimenContext(fixture)));

				var spec = constructorInfo.Invoke(parameters);

				await Task.WhenAll(testClass.Tests.Select(test => test.Pass()));

				await environment.Console.WriteLineAsync(specificationType.Name.Underscore().Titleize());
				
				switch (spec) {
					case ExceptionSpecification ex:
						_writer.Write(ex);
						break;
					case EventSpecification ev:
						_writer.Write(ev);
						break;
				}

				await environment.Console.WriteLineAsync();
			}

		}
	}
}
