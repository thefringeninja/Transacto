using AutoFixture;
using AutoFixture.Kernel;
using Fixie;

namespace Transacto;

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