using System.Text.Json;
using KellermanSoftware.CompareNetObjects;
using KellermanSoftware.CompareNetObjects.TypeComparers;
using Transacto.Testing;
using Transacto.Testing.Comparers;
using Xunit.Sdk;

namespace Transacto;

internal static class XUnitExtensions {
	private static IExceptionComparer CreateExceptionComparer() =>
		new CompareNetObjectsBasedExceptionComparer(new CompareLogic(new ComparisonConfig {
			MembersToIgnore =
				{ nameof(Exception.Source), nameof(Exception.StackTrace), nameof(Exception.TargetSite) }
		}));

	private static CompareNetObjectsBasedFactComparer CreateComparer() =>
		new(new CompareLogic {
			Config = {
				CustomComparers = [new JsonDocumentComparer(RootComparerFactory.GetRootComparer())],
				IgnoreCollectionOrder = true
			}
		});

	public static Task Assert(this IEventCentricTestSpecificationBuilder builder, object handler,
		IFactRecorder factRecorder) => builder.Assert(CreateComparer(), handler, factRecorder);

	public static Task Assert(this EventCentricTestSpecification specification, object handler,
		IFactRecorder factRecorder) => specification.Assert(CreateComparer(), handler, factRecorder);

	public static async Task Assert(this EventSpecification test, object handler, IFactRecorder factRecorder) =>
		await ((EventCentricTestSpecification)test).Assert(handler, factRecorder);

	/// <summary>
	///     Asserts that the specification is met.
	/// </summary>
	/// <param name="builder">The specification builder.</param>
	/// <param name="comparer">The event comparer.</param>
	/// <param name="handler"></param>
	/// <param name="factRecorder"></param>
	public static async Task Assert(this IEventCentricTestSpecificationBuilder builder,
		IFactComparer comparer, object handler, IFactRecorder factRecorder) {
		ArgumentNullException.ThrowIfNull(builder);
		await builder.Build().Assert(comparer, handler, factRecorder);
	}

	public static async Task Assert(this EventCentricTestSpecification specification, IFactComparer comparer,
		object handler, IFactRecorder factRecorder) {
		ArgumentNullException.ThrowIfNull(specification);
		ArgumentNullException.ThrowIfNull(comparer);
		ArgumentNullException.ThrowIfNull(handler);
		ArgumentNullException.ThrowIfNull(factRecorder);
		var runner = new EventCentricTestSpecificationRunner(comparer, handler, factRecorder);
		var result = await runner.Run(specification);

		if (!result.Failed) return;
		if (result.ButException.HasValue) {
			await using var writer = new StringWriter();
			writer.WriteLine("  Expected: {0} event(s),", result.Specification.Thens.Length);
			writer.WriteLine("  But was:  {0}", result.ButException.Value);

			throw new XunitException(writer.ToString());
		}

		if (!result.ButEvents.HasValue) return;

		if (result.ButEvents.Value.Length != result.Specification.Thens.Length) {
			await using var writer = new StringWriter();
			writer.WriteLine("  Expected: {0} event(s) ({1}),",
				result.Specification.Thens.Length,
				string.Join(",", result.Specification.Thens.Select(_ => _.Event.GetType().Name).ToArray()));
			writer.WriteLine("  But was:  {0} event(s) ({1})",
				result.ButEvents.Value.Length,
				string.Join(",", result.ButEvents.Value.Select(_ => _.Event.GetType().Name).ToArray()));

			throw new XunitException(writer.ToString());
		}

		await using (var writer = new StringWriter()) {
			writer.WriteLine("  Expected: {0} event(s) ({1}),",
				result.Specification.Thens.Length,
				string.Join(",", result.Specification.Thens.Select(_ => _.Event.GetType().Name).ToArray()));
			await writer.WriteLineAsync("  But found the following differences:");
			foreach (var difference in
			         result.Specification.Thens.Zip(result.ButEvents.Value,
					         (expected, actual) => new Tuple<Fact, Fact>(expected, actual))
				         .SelectMany(_ => comparer.Compare(_.Item1, _.Item2)))
				writer.WriteLine("    {0}", difference.Message);

			throw new XunitException(writer.ToString());
		}
	}


	public static Task Assert(this IExceptionCentricTestSpecificationBuilder builder, object handler,
		IFactRecorder factRecorder) => builder.Assert(CreateExceptionComparer(), handler, factRecorder);

	public static Task Assert(this ExceptionCentricTestSpecification specification, object handler,
		IFactRecorder factRecorder) => specification.Assert(CreateExceptionComparer(), handler, factRecorder);

	public static async Task Assert(this ExceptionSpecification test, object handler, IFactRecorder factRecorder) =>
		await ((ExceptionCentricTestSpecification)test).Assert(handler, factRecorder);

	public static async Task Assert(this IExceptionCentricTestSpecificationBuilder builder,
		IExceptionComparer comparer, object handler, IFactRecorder factRecorder) {
		ArgumentNullException.ThrowIfNull(builder);
		await builder.Build().Assert(comparer, handler, factRecorder);
	}

	public static async Task Assert(this ExceptionCentricTestSpecification specification, IExceptionComparer comparer,
		object handler, IFactRecorder factRecorder) {
		ArgumentNullException.ThrowIfNull(specification);
		ArgumentNullException.ThrowIfNull(comparer);
		ArgumentNullException.ThrowIfNull(handler);
		ArgumentNullException.ThrowIfNull(factRecorder);
		var runner = new ExceptionCentricTestSpecificationRunner(comparer, handler, factRecorder);
		var result = await runner.Run(specification);

		if (!result.Failed) return;
		if (result.ButException.HasValue) {
			await using var writer = new StringWriter();
			writer.WriteLine("  Expected: {0},", result.Specification.Throws);
			writer.WriteLine("  But was:  {0}", result.ButException.Value);

			throw new XunitException(writer.ToString());
		}

		if (!result.ButEvents.HasValue) return;

		await using (var writer = new StringWriter()) {
			writer.WriteLine("  Expected: {0},", result.Specification.Throws);
			writer.WriteLine("  But was:  {0} event(s) ({1})",
				result.ButEvents.Value.Length,
				string.Join(",", result.ButEvents.Value.Select(_ => _.GetType().Name).ToArray()));

			throw new XunitException(writer.ToString());
		}
	}

	private class JsonDocumentComparer(RootComparer rootComparer) : BaseTypeComparer(rootComparer) {
		public override bool IsTypeMatch(Type type1, Type type2) {
			return type1 == type2 && type1 == typeof(JsonDocument);
		}

		public override void CompareType(CompareParms parms) {
			if (parms.Object1.ToString() == parms.Object2.ToString()) return;

			AddDifference(parms);
		}
	}
}
