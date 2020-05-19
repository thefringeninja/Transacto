namespace Transacto.Plugins {
	public static class Standard {
		public static readonly IPlugin[] Plugins =
			{new BalanceSheet.BalanceSheet(), new GeneralLedger(), new ChartOfAccounts()};
	}
}
