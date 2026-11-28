using System.Reflection;
using AutoFixture.Idioms;
using AutoFixture.Kernel;

namespace Transacto;

public class ValueObjectAssertion<T>(ISpecimenBuilder builder) : CompositeIdiomaticAssertion(
	GetAssertions(builder)) {
	private static IEnumerable<IIdiomaticAssertion> GetAssertions(ISpecimenBuilder builder) {
		yield return new ValueObjectEqualityAssertion(builder);
		yield return new GuardClauseAssertion(builder);

		if (typeof(T).GetInterfaces().Contains(typeof(IComparable<T>))) {
			yield return new ValueObjectComparisonAssertion(builder);
		}
	}

	private class ValueObjectEqualityAssertion(ISpecimenBuilder builder) : CompositeIdiomaticAssertion(
		new EqualsNewObjectAssertion(builder),
		new EqualsSelfAssertion(builder),
		new EqualsSuccessiveAssertion(builder),
		new GetHashCodeSuccessiveAssertion(builder),
		new EqualityOperatorsAssertion(builder)) {
		private class EqualityOperatorsAssertion(ISpecimenBuilder builder) : CompositeIdiomaticAssertion(
			new EqualityOperatorAssertion(builder),
			new InequalityOperatorAssertion(builder)) {
			private class EqualityOperatorAssertion(ISpecimenBuilder builder)
				: OperatorAssertion(builder, "op_Equality") {
				protected override bool Expected(T left, T right) => EqualityComparer<T>.Default.Equals(left, right);
			}

			private class InequalityOperatorAssertion(ISpecimenBuilder builder)
				: OperatorAssertion(builder, "op_Inequality") {
				protected override bool Expected(T left, T right) => !EqualityComparer<T>.Default.Equals(left, right);
			}
		}
	}

	private class ValueObjectComparisonAssertion(ISpecimenBuilder builder) : CompositeIdiomaticAssertion(
		new LessThanOperatorAssertion(builder),
		new GreaterThanOperatorAssertion(builder),
		new LessThanOrEqualOperatorAssertion(builder),
		new GreaterThanOrEqualOperatorAssertion(builder)) {
		private class LessThanOperatorAssertion(ISpecimenBuilder builder)
			: OperatorAssertion(builder, "op_LessThan") {
			protected override bool Expected(T left, T right) => Comparer<T>.Default.Compare(left, right) < 0;
		}

		private class GreaterThanOperatorAssertion(ISpecimenBuilder builder)
			: OperatorAssertion(builder, "op_GreaterThan") {
			protected override bool Expected(T left, T right) => Comparer<T>.Default.Compare(left, right) > 0;
		}

		private class LessThanOrEqualOperatorAssertion(ISpecimenBuilder builder)
			: OperatorAssertion(builder, "op_LessThanOrEqual") {
			protected override bool Expected(T left, T right) => Comparer<T>.Default.Compare(left, right) <= 0;
		}

		private class GreaterThanOrEqualOperatorAssertion(ISpecimenBuilder builder)
			: OperatorAssertion(builder, "op_GreaterThanOrEqual") {
			protected override bool Expected(T left, T right) => Comparer<T>.Default.Compare(left, right) >= 0;
		}
	}

	private abstract class OperatorAssertion(ISpecimenBuilder builder, string operatorName) : IdiomaticAssertion {
		protected abstract bool Expected(T left, T right);

		public override void Verify(ConstructorInfo constructorInfo) {
			var @operator = typeof(T).GetMethod(operatorName, BindingFlags.Public | BindingFlags.Static,
				[typeof(T), typeof(T)]);

			if (@operator is null || @operator.ReturnType != typeof(bool)) {
				Assert.Fail($"{typeof(T)} does not define a public static bool {operatorName}({typeof(T).Name}, {typeof(T).Name}).");
				return;
			}

			var context = new SpecimenContext(builder);
			var a = (T)context.Resolve(typeof(T));
			var b = (T)context.Resolve(typeof(T));

			foreach (var (left, right) in new[] { (a, a), (a, b), (b, a) }) {
				var actual = (bool)@operator.Invoke(null, [left, right])!;
				Assert.True(Expected(left, right) == actual,
					$"{typeof(T)}.{operatorName}({left}, {right}) returned {actual}, expected {!actual}.");
			}
		}
	}
}
