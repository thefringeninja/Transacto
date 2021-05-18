using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Transacto.Framework;

namespace Transacto.Testing {
    public class ExceptionCentricTestSpecificationRunner : IExceptionCentricTestSpecificationRunner {
        private readonly IExceptionComparer _comparer;
        private readonly object _handler;
        private readonly IFactRecorder _factRecorder;

        public ExceptionCentricTestSpecificationRunner(IExceptionComparer comparer, object handler,
            IFactRecorder factRecorder) {
            if (comparer == null) throw new ArgumentNullException(nameof(comparer));
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (factRecorder == null) throw new ArgumentNullException(nameof(factRecorder));
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

            ValueTask Handler(object o, CancellationToken ct) => (ValueTask)methodInfo.Invoke(_handler, new[] {o, ct})!;

            _factRecorder.Record(specification.Givens);

            Optional<Exception> optionalException = Optional<Exception>.Empty;

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
}
