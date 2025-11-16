using AutoFixture;

namespace Transacto.Domain;

public abstract class ValueObjectTests<T> {
	protected Fixture Fixture { get; } = new ScenarioFixture();

	public void ValueObjectAssertions() => Fixture.Create<ValueObjectAssertion>()
		.Verify(typeof(T).GetConstructors());
}
