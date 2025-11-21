namespace Transacto.Plugins;

public static class Standard {
	public static readonly IPlugin[] Plugins = [
		new ChartOfAccounts.ChartOfAccounts(),
		new GeneralLedger.GeneralLedger(),
		new FinancialStatements.FinancialStatements()
	];
}
