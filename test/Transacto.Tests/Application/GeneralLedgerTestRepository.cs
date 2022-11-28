using Transacto.Domain;
using Transacto.Testing;

namespace Transacto.Application;

internal class GeneralLedgerTestRepository : IGeneralLedgerRepository {
	private readonly FactRecorderRepository<GeneralLedger> _inner;

	public GeneralLedgerTestRepository(IFactRecorder factRecorder) {
		_inner = new FactRecorderRepository<GeneralLedger>(factRecorder);
	}

	public async ValueTask<GeneralLedger> Get(CancellationToken cancellationToken = default) {
		var optional = await _inner.GetOptional(GeneralLedger.Identifier, cancellationToken);
		if (optional.HasValue) {
			return optional.Value;
		}

		var generalLedger = GeneralLedger.Factory();
		_inner.Add(generalLedger);
		return generalLedger;
	}

	public void Add(GeneralLedger generalLedger) => _inner.Add(generalLedger);
}
