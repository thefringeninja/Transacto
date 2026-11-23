using Transacto.Framework;

namespace Transacto.Testing;

public class EventCentricTestSpecificationRunner : IEventCentricTestSpecificationRunner {
	private readonly IFactComparer _comparer;
	private readonly IFactRecorder _factRecorder;
	private readonly object _handler;

	public EventCentricTestSpecificationRunner(IFactComparer comparer, object handler, IFactRecorder factRecorder) {
		ArgumentNullException.ThrowIfNull(comparer);
		ArgumentNullException.ThrowIfNull(handler);
		ArgumentNullException.ThrowIfNull(factRecorder);
		_comparer = comparer;
		_factRecorder = factRecorder;
		_handler = handler;
	}

	public async Task<EventCentricTestResult> Run(EventCentricTestSpecification specification) {
		var methodInfo = _handler.GetType().GetMethods().Single(
			mi => {
				var parameters = mi.GetParameters();
				return mi.IsPublic &&
				       mi.ReturnType == typeof(ValueTask) &&
				       parameters.Length == 2 &&
				       parameters[0].ParameterType == specification.When.GetType() &&
				       parameters[1].ParameterType == typeof(CancellationToken);
			});

		ValueTask Handler(object o, CancellationToken ct) {
			return (ValueTask)methodInfo.Invoke(_handler, [o, ct])!;
		}

		_factRecorder.Record(specification.Givens);

		var optionalException = Option<Exception>.None;

		try {
			await Handler(specification.When, CancellationToken.None);
		} catch (Exception ex) {
			optionalException = Option<Exception>.Some(ex);
		}

		var then = _factRecorder.GetFacts().Skip(specification.Givens.Length).ToArray();
		return new EventCentricTestResult(specification,
			optionalException.IsNone &&
			specification.Thens.SequenceEqual(then, new FactEqualityComparer(_comparer))
				? TestResultState.Passed
				: TestResultState.Failed,
			Option<Fact[]>.Some(then), optionalException);
	}
}
