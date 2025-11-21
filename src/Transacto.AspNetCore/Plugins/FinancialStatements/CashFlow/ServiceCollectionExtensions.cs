// ReSharper disable CheckNamespace

using System.Data.SQLite;
using Transacto.Plugins.FinancialStatements.CashFlow;

namespace Microsoft.Extensions.DependencyInjection;
// ReSharper restore CheckNamespace

static partial class ServiceCollectionExtensions {
	public static IServiceCollection AddCashFlow(this IServiceCollection services) => services
		.AddSQLiteProjection<CashFlowProjection>()
		.AddSingleton<GetCashFlowReportsAsync>(provider => {
			var connectionFactory =
				provider.GetRequiredKeyedService<Func<SQLiteConnection>>(typeof(CashFlowProjection));
			return async (reportRanges, ct) =>
				await Services.CashFlowReportsAsync(connectionFactory, reportRanges, ct);
		});
}
