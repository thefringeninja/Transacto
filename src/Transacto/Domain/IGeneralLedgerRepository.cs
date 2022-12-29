namespace Transacto.Domain;

public interface IGeneralLedgerRepository {
	ValueTask<GeneralLedger> Get(CancellationToken cancellationToken = default);
	void Add(GeneralLedger generalLedger);
}
