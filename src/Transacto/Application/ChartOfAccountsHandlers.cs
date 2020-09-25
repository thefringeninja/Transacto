using System.Threading;
using System.Threading.Tasks;
using Transacto.Domain;
using Transacto.Messages;

namespace Transacto.Application {
	public class ChartOfAccountsHandlers {
		private readonly IChartOfAccountsRepository _chartOfAccounts;

		public ChartOfAccountsHandlers(IChartOfAccountsRepository chartOfAccounts) {
			_chartOfAccounts = chartOfAccounts;
		}

        public async ValueTask Handle(DefineAccount command, CancellationToken cancellationToken = default) {
            var optionalChart = await _chartOfAccounts.GetOptional(cancellationToken);

			var chart = optionalChart.HasValue ? optionalChart.Value : ChartOfAccounts.Factory();

			chart.DefineAccount(new AccountName(command.AccountName!), new AccountNumber(command.AccountNumber));

			if (!optionalChart.HasValue) {
				_chartOfAccounts.Add(chart);
			}
		}

        public async ValueTask Handle(RenameAccount command, CancellationToken cancellationToken = default) {
            var chart = await _chartOfAccounts.Get(cancellationToken);

			chart.RenameAccount(new AccountNumber(command.AccountNumber), new AccountName(command.NewAccountName!));
		}

        public async ValueTask Handle(DeactivateAccount command, CancellationToken cancellationToken = default) {
            var chart = await _chartOfAccounts.Get(cancellationToken);

			chart.DeactivateAccount(new AccountNumber(command.AccountNumber));
		}

        public async ValueTask Handle(ReactivateAccount command, CancellationToken cancellationToken = default) {
            var chart = await _chartOfAccounts.Get(cancellationToken);

			chart.ReactivateAccount(new AccountNumber(command.AccountNumber));
		}
	}
}
