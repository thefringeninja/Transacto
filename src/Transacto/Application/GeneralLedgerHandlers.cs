using System.Collections.Immutable;
using NodaTime;
using Transacto.Domain;
using Transacto.Messages;

namespace Transacto.Application;

public class GeneralLedgerHandlers(
	IGeneralLedgerRepository ledger,
	IChartOfAccountsRepository ofAccounts) {
	public ValueTask Handle(OpenGeneralLedger command, CancellationToken cancellationToken = default) {
		ledger.Add(GeneralLedger.Open(command.OpenedOn));

		return new ValueTask(Task.CompletedTask);
	}

	public async ValueTask Handle(BeginClosingAccountingPeriod command,
		CancellationToken cancellationToken = default) {
		var generalLedger = await ledger.Get(cancellationToken);
		var chartOfAccounts = await ofAccounts.Get(cancellationToken);

		generalLedger.BeginClosingPeriod(
			chartOfAccounts.Get<EquityAccount>(new AccountNumber(command.RetainedEarningsAccountNumber)),
			new GeneralLedgerEntryIdentifier(command.ClosingGeneralLedgerEntryId),
			ImmutableArray.CreateRange(command.GeneralLedgerEntryIds, id => new GeneralLedgerEntryIdentifier(id)),
			command.ClosingOn);
	}
}
