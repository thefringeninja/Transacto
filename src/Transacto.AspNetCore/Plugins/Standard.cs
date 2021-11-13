namespace Transacto.Plugins; 

public static class Standard {
	public static readonly IPlugin[] Plugins = {
		new ChartOfAccounts.ChartOfAccounts(),
		new BalanceSheet.BalanceSheet(),
		new GeneralLedger.GeneralLedger()
	};
}