using System;
using System.Threading;
using System.Threading.Tasks;
using Transacto.Domain;
using Transacto.Messages;

namespace Transacto.Application {
	public class AccountingPeriodClosingHandlers {
		private readonly IGeneralLedgerRepository _generalLedger;
		private readonly IGeneralLedgerEntryRepository _generalLedgerEntries;
		private readonly IChartOfAccountsRepository _chartOfAccounts;
		private readonly AccountIsDeactivated _accountIsDeactivated;

		public AccountingPeriodClosingHandlers(IGeneralLedgerRepository generalLedger,
			IGeneralLedgerEntryRepository generalLedgerEntries,
			IChartOfAccountsRepository chartOfAccounts,
			AccountIsDeactivated accountIsDeactivated) {
			_generalLedger = generalLedger;
			_generalLedgerEntries = generalLedgerEntries;
			_chartOfAccounts = chartOfAccounts;
			_accountIsDeactivated = accountIsDeactivated;
		}

		public async ValueTask Handle(AccountingPeriodClosing @event, CancellationToken cancellationToken) {
			var generalLedgerEntryIdentifiers =
				Array.ConvertAll(@event.GeneralLedgerEntryIds, id => new GeneralLedgerEntryIdentifier(id));

			var chartOfAccounts = await _chartOfAccounts.Get(cancellationToken);

			var accountingPeriodClosingProcess = new AccountingPeriodClosingProcess(
				chartOfAccounts, AccountingPeriod.Parse(@event.Period), Time.Parse.LocalDateTime(@event.ClosingOn),
				generalLedgerEntryIdentifiers, new GeneralLedgerEntryIdentifier(@event.ClosingGeneralLedgerEntryId),
				(EquityAccount)chartOfAccounts[new AccountNumber(@event.RetainedEarningsAccountNumber)],
				_accountIsDeactivated);

			foreach (var id in @event.GeneralLedgerEntryIds) {
				var generalLedgerEntry =
					await _generalLedgerEntries.Get(new GeneralLedgerEntryIdentifier(id), cancellationToken);
				accountingPeriodClosingProcess.TransferEntry(generalLedgerEntry);
			}

			var generalLedger = await _generalLedger.Get(cancellationToken);

			generalLedger.CompleteClosingPeriod(generalLedgerEntryIdentifiers,
				accountingPeriodClosingProcess.Complete(), accountingPeriodClosingProcess.TrialBalance);
		}
	}
}
