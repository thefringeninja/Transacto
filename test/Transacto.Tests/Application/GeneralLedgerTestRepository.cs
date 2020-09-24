using System;
using System.Threading;
using System.Threading.Tasks;
using Transacto.Domain;
using Transacto.Testing;

namespace Transacto.Application {
	internal class GeneralLedgerTestRepository : IGeneralLedgerRepository {
		private readonly FactRecorderRepository<GeneralLedger> _inner;

		public GeneralLedgerTestRepository(IFactRecorder factRecorder) {
			_inner = new FactRecorderRepository<GeneralLedger>(factRecorder, GeneralLedger.Factory);
		}

		public async ValueTask<GeneralLedger> Get(CancellationToken cancellationToken = default) {
			var optional = await _inner.GetOptional(GeneralLedger.Identifier, cancellationToken);
			if (!optional.HasValue) {
				throw new InvalidOperationException();
			}

			return optional.Value;
		}

		public void Add(GeneralLedger generalLedger) => _inner.Add(generalLedger);
	}
}
