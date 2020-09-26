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
			var retainedEarningsAccountNumber = new AccountNumber(@event.RetainedEarningsAccountNumber);
			AccountType.OfAccountNumber(retainedEarningsAccountNumber).MustBe(AccountType.Equity);
			var generalLedger = await _generalLedger.Get(cancellationToken);
			foreach (var id in @event.GeneralLedgerEntryIds) {
				var generalLedgerEntry =
					await _generalLedgerEntries.Get(new GeneralLedgerEntryIdentifier(id), cancellationToken);
				generalLedger.TransferEntry(generalLedgerEntry);
			}

			generalLedger.CompleteClosingPeriod(_accountIsDeactivated, retainedEarningsAccountNumber);
		}
	}
}
