using System.Collections.Immutable;
using Transacto.Domain;
using Transacto.Messages;

namespace Transacto.Application;

public class AccountingPeriodClosingHandlers(
	IGeneralLedgerRepository ledger,
	IGeneralLedgerEntryRepository generalLedgerEntries,
	IChartOfAccountsRepository ofAccounts,
	AccountIsDeactivated accountIsDeactivated) {
	public async ValueTask Handle(AccountingPeriodClosing @event, CancellationToken cancellationToken) {
		var generalLedgerEntryIdentifiers = ImmutableHashSet.CreateRange(
			ImmutableArray.CreateRange(@event.GeneralLedgerEntryIds, id => new GeneralLedgerEntryIdentifier(id)));

		var chartOfAccounts = await ofAccounts.Get(cancellationToken);

		var accountingPeriodClosingProcess = new AccountingPeriodClosingProcess(
			chartOfAccounts, AccountingPeriod.Parse(@event.Period), Time.Parse.LocalDateTime(@event.ClosingOn),
			generalLedgerEntryIdentifiers, new GeneralLedgerEntryIdentifier(@event.ClosingGeneralLedgerEntryId),
			chartOfAccounts.Get<EquityAccount>(new AccountNumber(@event.RetainedEarningsAccountNumber)),
			accountIsDeactivated);

		foreach (var identifier in generalLedgerEntryIdentifiers) {
			var generalLedgerEntry = await generalLedgerEntries.Get(identifier, cancellationToken);
			accountingPeriodClosingProcess.TransferEntry(generalLedgerEntry);
		}

		var generalLedger = await ledger.Get(cancellationToken);

		generalLedger.CompleteClosingPeriod(generalLedgerEntryIdentifiers,
			accountingPeriodClosingProcess.Complete(), accountingPeriodClosingProcess.TrialBalance);
	}
}
