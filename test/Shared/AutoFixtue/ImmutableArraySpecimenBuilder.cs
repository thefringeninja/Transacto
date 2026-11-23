using System.Collections.Immutable;
using AutoFixture.Kernel;

namespace Transacto;

internal class ImmutableArraySpecimenBuilder : ISpecimenBuilder {
	public object Create(object request, ISpecimenContext context) =>
		request is Type { IsGenericType: true } type && type.GetGenericTypeDefinition() == typeof(ImmutableArray<>)
			? typeof(ImmutableArray).GetMethods()
				.Single(m => m is { Name: nameof(ImmutableArray.CreateRange), IsGenericMethodDefinition: true } &&
				             m.GetParameters() is [{ ParameterType.Name: "IEnumerable`1" }])
				.MakeGenericMethod(type.GenericTypeArguments[0])
				.Invoke(null, [context.Resolve(typeof(IEnumerable<>).MakeGenericType(type.GenericTypeArguments[0]))])!
			: new NoSpecimen();
}
