namespace Transacto.Plugins;

public static class Standard {

	public static IPlugin ChartOfAccounts { get; } = new ChartOfAccounts.ChartOfAccounts();
	public static IPlugin GeneralLedger { get; } = new GeneralLedger.GeneralLedger();
	public static IPlugin FinancialStatements { get; } = new FinancialStatements.FinancialStatements();
	
	public static readonly IPlugin[] Plugins = [
		ChartOfAccounts,
		GeneralLedger,
		FinancialStatements
	];
}
