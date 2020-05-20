using System;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client;
using Transacto.Domain;
using Transacto.Messages;

namespace Transacto.Application {
	public class GeneralLedgerHandlers {
		private readonly IGeneralLedgerRepository _generalLedger;
		private readonly IGeneralLedgerEntryRepository _generalLedgerEntries;
		private readonly AccountIsDeactivated _accountIsDeactivated;

		public GeneralLedgerHandlers(IGeneralLedgerRepository generalLedger,
			IGeneralLedgerEntryRepository generalLedgerEntries,
			AccountIsDeactivated accountIsDeactivated) {
			_generalLedger = generalLedger;
			_generalLedgerEntries = generalLedgerEntries;
			_accountIsDeactivated = accountIsDeactivated;
		}

		public ValueTask Handle(OpenGeneralLedger command, CancellationToken cancellationToken = default) {
			_generalLedger.Add(GeneralLedger.Open(command.OpenedOn));

			return new ValueTask(Task.CompletedTask);
		}

		public async ValueTask Handle(BeginClosingAccountingPeriod command,
			CancellationToken cancellationToken = default) {
			var generalLedger = await _generalLedger.Get(cancellationToken);

			var retainedEarningsAccountNumber = new AccountNumber(command.RetainedEarningsAccountNumber);
			AccountType.OfAccountNumber(retainedEarningsAccountNumber).MustBe(AccountType.Equity);

			generalLedger.BeginClosingPeriod(retainedEarningsAccountNumber,
				new GeneralLedgerEntryIdentifier(command.ClosingGeneralLedgerEntryId),
				Array.ConvertAll(command.GeneralLedgerEntryIds, id => new GeneralLedgerEntryIdentifier(id)),
				command.ClosingOn);
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
