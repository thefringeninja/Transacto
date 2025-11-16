using AutoFixture.Idioms;
using AutoFixture.Kernel;

namespace Transacto;

public class ValueObjectAssertion(ISpecimenBuilder builder) : CompositeIdiomaticAssertion(
	new EqualsNewObjectAssertion(builder), new EqualsNewObjectAssertion(builder), new EqualsSelfAssertion(builder),
	new EqualsSuccessiveAssertion(builder), new GetHashCodeSuccessiveAssertion(builder),
	new GuardClauseAssertion(builder));
