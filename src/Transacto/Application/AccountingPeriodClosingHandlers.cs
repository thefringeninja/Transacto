using System;
using System.Threading;
using System.Threading.Tasks;
using Transacto.Domain;
using Transacto.Messages;

namespace Transacto.Application {
	public class AccountingPeriodClosingHandlers {
		private readonly IGeneralLedgerRepository _generalLedger;
		private readonly IGeneralLedgerEntryRepository _generalLedgerEntries;
		private readonly AccountIsDeactivated _accountIsDeactivated;

		public AccountingPeriodClosingHandlers(IGeneralLedgerRepository generalLedger,
			IGeneralLedgerEntryRepository generalLedgerEntries,
			AccountIsDeactivated accountIsDeactivated) {
			_generalLedger = generalLedger;
			_generalLedgerEntries = generalLedgerEntries;
			_accountIsDeactivated = accountIsDeactivated;
		}

		public async ValueTask Handle(AccountingPeriodClosing @event, CancellationToken cancellationToken) {
			var generalLedgerEntryIdentifiers =
				Array.ConvertAll(@event.GeneralLedgerEntryIds, id => new GeneralLedgerEntryIdentifier(id));


			var accountingPeriodClosingProcess = new AccountingPeriodClosingProcess(
				Period.Parse(@event.Period), Time.Parse.LocalDateTime(@event.ClosingOn), generalLedgerEntryIdentifiers,
				new GeneralLedgerEntryIdentifier(@event.ClosingGeneralLedgerEntryId),
				new AccountNumber(@event.RetainedEarningsAccountNumber), _accountIsDeactivated);

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
