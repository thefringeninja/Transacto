using Transacto.Domain;
using Transacto.Messages;

namespace Transacto.Application;

public class ChartOfAccountsHandlers(IChartOfAccountsRepository chartOfAccounts) {
	public async ValueTask Handle(ConfigureChartOfAccounts command, CancellationToken cancellationToken = default) {
		var optionalChart = await chartOfAccounts.GetOptional(cancellationToken);

		if (optionalChart.HasValue)
			throw new ChartOfAccountsAlreadyConfiguredException();

		var chart = ChartOfAccounts.Configure(new ChartOfAccountsSettings(
			new(command.AccountNumberWidth),
			new(command.AccountsReceivable),
			new(command.AccountsPayable),
			new(command.RetainedEarnings)));

		chartOfAccounts.Add(chart);
	}

	public async ValueTask Handle(DefineAccount command, CancellationToken cancellationToken = default) {
		var chart = await chartOfAccounts.Get(cancellationToken);

		chart.DefineAccount(new AccountName(command.AccountName), new AccountNumber(command.AccountNumber),
			CashFlowCategory.FromCashFlowCategory(command.CashFlowCategory), command.AffectsCash);
	}


	public async ValueTask Handle(RenameAccount command, CancellationToken cancellationToken = default) {
		var chart = await chartOfAccounts.Get(cancellationToken);

		chart.RenameAccount(new AccountNumber(command.AccountNumber), new AccountName(command.NewAccountName));
	}

	public async ValueTask Handle(DeactivateAccount command, CancellationToken cancellationToken = default) {
		var chart = await chartOfAccounts.Get(cancellationToken);

		chart.DeactivateAccount(new AccountNumber(command.AccountNumber));
	}

	public async ValueTask Handle(ReactivateAccount command, CancellationToken cancellationToken = default) {
		var chart = await chartOfAccounts.Get(cancellationToken);

		chart.ReactivateAccount(new AccountNumber(command.AccountNumber));
	}
}
