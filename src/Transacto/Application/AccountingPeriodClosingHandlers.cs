using System.Collections.Immutable;
using NodaTime;
using Transacto.Domain;
using Transacto.Messages;

namespace Transacto.Application;

public class AccountingPeriodClosingHandlers(
	IGeneralLedgerRepository ledger,
	IGeneralLedgerEntryRepository generalLedgerEntries,
	IChartOfAccountsRepository ofAccounts) {
	public async ValueTask Handle(AccountingPeriodClosing @event, CancellationToken cancellationToken) {
		var accountingPeriod = AccountingPeriod.Parse(@event.Period);

		var generalLedger = await ledger.Get(cancellationToken);

		// The closing event may be redelivered after the period was already closed; that is a no-op.
		if (generalLedger.HasClosed(accountingPeriod)) {
			return;
		}

		var generalLedgerEntryIdentifiers = ImmutableHashSet.CreateRange(
			ImmutableArray.CreateRange(@event.GeneralLedgerEntryIds, id => new GeneralLedgerEntryIdentifier(id)));

		var chartOfAccounts = await ofAccounts.Get(cancellationToken);

		var accountingPeriodClosingProcess = new AccountingPeriodClosingProcess(
			chartOfAccounts, accountingPeriod, @event.ClosingOn,
			generalLedgerEntryIdentifiers, new GeneralLedgerEntryIdentifier(@event.ClosingGeneralLedgerEntryId),
			chartOfAccounts.RetainedEarnings);

		foreach (var identifier in generalLedgerEntryIdentifiers) {
			var generalLedgerEntry = await generalLedgerEntries.Get(identifier, cancellationToken);
			accountingPeriodClosingProcess.TransferEntry(generalLedgerEntry);
		}

		generalLedger.CompleteClosingPeriod(accountingPeriod, generalLedgerEntryIdentifiers,
			accountingPeriodClosingProcess.Complete(), accountingPeriodClosingProcess.TrialBalance);
	}
}
