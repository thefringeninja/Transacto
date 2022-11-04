using System.Threading;
using System.Threading.Tasks;
using EventStore.Client;
using Transacto.Domain;
using Transacto.Framework;

namespace Transacto.Infrastructure.EventStore; 

public class ChartOfAccountsEventStoreRepository : IChartOfAccountsRepository {
	private readonly EventStoreRepository<ChartOfAccounts> _inner;

	public ChartOfAccountsEventStoreRepository(EventStoreClient eventStore, IMessageTypeMapper messageTypeMapper) {
		_inner = new EventStoreRepository<ChartOfAccounts>(eventStore,
			ChartOfAccounts.Factory, messageTypeMapper);
	}

	public ValueTask<Optional<ChartOfAccounts>> GetOptional(CancellationToken cancellationToken = default)
		=> _inner.GetById(ChartOfAccounts.Identifier, cancellationToken);

	public async ValueTask<ChartOfAccounts> Get(CancellationToken cancellationToken = default) =>
		await GetOptional(cancellationToken) switch {
			{ HasValue: true } optional => optional.Value,
			_ => throw new ChartOfAccountsNotFoundException()
		};

	public void Add(ChartOfAccounts chartOfAccounts) => _inner.Add(chartOfAccounts);
}
