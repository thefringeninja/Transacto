// ReSharper disable CheckNamespace

using System.Data.SQLite;
using Transacto.Plugins.FinancialStatements.CashFlow;
using Transacto.Plugins.FinancialStatements.ProfitAndLoss;
using Services = Transacto.Plugins.FinancialStatements.CashFlow.Services;

namespace Microsoft.Extensions.DependencyInjection;
// ReSharper restore CheckNamespace

static partial class ServiceCollectionExtensions {
	public static IServiceCollection AddCashFlow(this IServiceCollection services) => services
		.AddSQLiteProjection<CashFlowProjection>()
		.AddSingleton<GetCashFlowReportsAsync>(provider => {
			var connectionFactory =
				provider.GetRequiredKeyedService<Func<SQLiteConnection>>(typeof(CashFlowProjection));

			var getProfitAndLossReports = provider.GetRequiredService<GetProfitAndLossReportsAsync>();
			return async (reportRanges, ct) =>
				await Services.CashFlowReportsAsync(connectionFactory, getProfitAndLossReports, reportRanges, ct);
		})
		.AddTransient<CashFlowReportsRepresentation>();
}
