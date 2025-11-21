using System.Data.SQLite;
using Transacto.Plugins.FinancialStatements.ProfitAndLoss;

// ReSharper disable CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;
// ReSharper restore CheckNamespace

static partial class ServiceCollectionExtensions {
	public static IServiceCollection AddProfitAndLoss(this IServiceCollection services) => services
		.AddSQLiteProjection<ProfitAndLossProjection>()
		.AddSingleton<GetProfitAndLossReportsAsync>(provider => {
			var connectionFactory =
				provider.GetRequiredKeyedService<Func<SQLiteConnection>>(typeof(ProfitAndLossProjection));
			return async (reportRanges, ct) =>
				await Services.ProfitAndLossReportsAsync(connectionFactory, reportRanges, ct);
		});
}
