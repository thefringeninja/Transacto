using Transacto.Framework;

namespace Transacto.Testing;

public class ExceptionCentricTestSpecificationRunner : IExceptionCentricTestSpecificationRunner {
	private readonly IExceptionComparer _comparer;
	private readonly IFactRecorder _factRecorder;
	private readonly object _handler;

	public ExceptionCentricTestSpecificationRunner(IExceptionComparer comparer, object handler,
		IFactRecorder factRecorder) {
		ArgumentNullException.ThrowIfNull(comparer);
		ArgumentNullException.ThrowIfNull(handler);
		ArgumentNullException.ThrowIfNull(factRecorder);
		_comparer = comparer;
		_factRecorder = factRecorder;
		_handler = handler;
	}

	public async Task<ExceptionCentricTestResult> Run(ExceptionCentricTestSpecification specification) {
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

		var optionalException = Optional<Exception>.Empty;

		try {
			await Handler(specification.When, CancellationToken.None);
		} catch (Exception ex) {
			optionalException = ex;
		}

		var then = _factRecorder.GetFacts().Skip(specification.Givens.Length).ToArray();
		return new ExceptionCentricTestResult(specification,
			optionalException.HasValue &&
			!_comparer.Compare(specification.Throws, optionalException.Value).Any()
				? TestResultState.Passed
				: TestResultState.Failed,
			optionalException, then);
	}
}
