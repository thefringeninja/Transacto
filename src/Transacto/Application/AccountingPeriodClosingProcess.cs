using System.Threading;
using System.Threading.Tasks;
using Transacto.Domain;
using Transacto.Messages;

namespace Transacto.Application {
	public class AccountingPeriodClosingProcess {
		private readonly IGeneralLedgerRepository _generalLedger;
		private readonly IGeneralLedgerEntryRepository _generalLedgerEntries;
		private readonly IChartOfAccountsRepository _chartOfAccounts;

		public AccountingPeriodClosingProcess(IGeneralLedgerRepository generalLedger,
			IGeneralLedgerEntryRepository generalLedgerEntries,
			IChartOfAccountsRepository chartOfAccounts) {
			_generalLedger = generalLedger;
			_generalLedgerEntries = generalLedgerEntries;
			_chartOfAccounts = chartOfAccounts;
		}

		public async ValueTask Handle(AccountingPeriodClosing @event, CancellationToken cancellationToken) {
			var retainedEarningsAccountNumber = new AccountNumber(@event.RetainedEarningsAccountNumber);
			AccountType.OfAccountNumber(retainedEarningsAccountNumber).MustBe(AccountType.Equity);
			var generalLedger = await _generalLedger.Get(cancellationToken);
			foreach (var id in @event.GeneralLedgerEntryIds) {
				var generalLedgerEntry =
					await _generalLedgerEntries.Get(new GeneralLedgerEntryIdentifier(id), cancellationToken);
				generalLedger.TransferEntry(generalLedgerEntry);
			}

			var chartOfAccounts = await _chartOfAccounts.Get(cancellationToken);

			generalLedger.CompleteClosingPeriod(chartOfAccounts, retainedEarningsAccountNumber);
		}
	}
}
