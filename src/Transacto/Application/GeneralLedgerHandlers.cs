using System.Collections.Immutable;
using NodaTime;
using Transacto.Domain;
using Transacto.Messages;

namespace Transacto.Application; 

public class GeneralLedgerHandlers {
	private readonly IGeneralLedgerRepository _generalLedger;
	private readonly IChartOfAccountsRepository _chartOfAccounts;

	public GeneralLedgerHandlers(IGeneralLedgerRepository generalLedger,
		IChartOfAccountsRepository chartOfAccounts) {
		_generalLedger = generalLedger;
		_chartOfAccounts = chartOfAccounts;
	}

	public ValueTask Handle(OpenGeneralLedger command, CancellationToken cancellationToken = default) {
		_generalLedger.Add(GeneralLedger.Open(LocalDate.FromDateTime(command.OpenedOn.LocalDateTime)));

		return new ValueTask(Task.CompletedTask);
	}

	public async ValueTask Handle(BeginClosingAccountingPeriod command,
		CancellationToken cancellationToken = default) {
		var generalLedger = await _generalLedger.Get(cancellationToken);
		var chartOfAccounts = await _chartOfAccounts.Get(cancellationToken);

		generalLedger.BeginClosingPeriod(
			(EquityAccount)chartOfAccounts[new AccountNumber(command.RetainedEarningsAccountNumber)],
			new GeneralLedgerEntryIdentifier(command.ClosingGeneralLedgerEntryId),
			ImmutableArray.CreateRange(command.GeneralLedgerEntryIds, id => new GeneralLedgerEntryIdentifier(id)),
			LocalDateTime.FromDateTime(command.ClosingOn.DateTime));
	}
}
