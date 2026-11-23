using Transacto.Domain;
using Transacto.Framework;
using Transacto.Testing;

namespace Transacto.Application;

internal class GeneralLedgerTestRepository(IFactRecorder factRecorder) : IGeneralLedgerRepository {
	private readonly FactRecorderRepository<GeneralLedger> _inner = new(factRecorder);

	public async ValueTask<GeneralLedger> Get(CancellationToken cancellationToken = default) {
		var optional = await _inner.GetOptional(GeneralLedger.Identifier, cancellationToken);
		
		if (optional is Some<GeneralLedger> { Value: var value }) {
			return value;
		}

		var generalLedger = GeneralLedger.Factory();
		_inner.Add(generalLedger);
		return generalLedger;
	}

	public void Add(GeneralLedger generalLedger) => _inner.Add(generalLedger);
}
