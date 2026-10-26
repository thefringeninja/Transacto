using KurrentDB.Client;
using Transacto.Domain;
using Transacto.Framework;

namespace Transacto.Infrastructure.EventStore;

public class ChartOfAccountsEventStoreRepository(KurrentDBClient eventStore, IMessageTypeMapper messageTypeMapper)
	: IChartOfAccountsRepository {
	private readonly EventStoreRepository<ChartOfAccounts> _inner = new(eventStore, messageTypeMapper);

	public ValueTask<Option<ChartOfAccounts>> GetOptional(CancellationToken cancellationToken = default)
		=> _inner.GetById(ChartOfAccounts.Identifier, cancellationToken);

	public async ValueTask<ChartOfAccounts> Get(CancellationToken cancellationToken = default) =>
		await GetOptional(cancellationToken) switch {
			Some<ChartOfAccounts> some => some.Value,
			_ => throw new ChartOfAccountsNotFoundException()
		};

	public void Add(ChartOfAccounts chartOfAccounts) => _inner.Add(chartOfAccounts);
}
