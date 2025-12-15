using Transacto.Framework;

namespace Transacto.Plugins;

public static class Standard {
	public static IPlugin ChartOfAccounts { get; } = new ChartOfAccounts.ChartOfAccounts();
	public static IPlugin GeneralLedger { get; } = new GeneralLedger.GeneralLedger();
	public static IPlugin FinancialStatements { get; } = new FinancialStatements.FinancialStatements();

#if DEBUG
	public static IPlugin SchemaGenerator { get; } = new Framework.SchemaGenerator.SchemaGenerator();
#endif
	public static readonly IPlugin[] Plugins = [
		ChartOfAccounts,
		GeneralLedger,
		FinancialStatements,
#if DEBUG
		SchemaGenerator,
#endif
	];
}
