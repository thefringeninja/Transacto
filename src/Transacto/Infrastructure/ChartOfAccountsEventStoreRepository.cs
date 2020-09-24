using System;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client;
using Transacto.Domain;
using Transacto.Framework;
using Transacto.Framework.CommandHandling;

namespace Transacto.Infrastructure {
	public class ChartOfAccountsEventStoreRepository : IChartOfAccountsRepository {
		private readonly EventStoreRepository<ChartOfAccounts> _inner;

		public ChartOfAccountsEventStoreRepository(EventStoreClient eventStore,
			IMessageTypeMapper messageTypeMapper, UnitOfWork unitOfWork) {
			_inner = new EventStoreRepository<ChartOfAccounts>(eventStore, unitOfWork,
				ChartOfAccounts.Factory, messageTypeMapper);
		}

		public ValueTask<Optional<ChartOfAccounts>> GetOptional(CancellationToken cancellationToken = default)
			=> _inner.GetById(ChartOfAccounts.Identifier, cancellationToken);

		public async ValueTask<ChartOfAccounts> Get(CancellationToken cancellationToken = default) {
			var optionalChartOfAccounts = await GetOptional(cancellationToken);
			if (!optionalChartOfAccounts.HasValue) {
				throw new ChartOfAccountsNotFoundException();
			}

			return optionalChartOfAccounts.Value;
		}

		public void Add(ChartOfAccounts chartOfAccounts) => _inner.Add(chartOfAccounts);
	}
}
