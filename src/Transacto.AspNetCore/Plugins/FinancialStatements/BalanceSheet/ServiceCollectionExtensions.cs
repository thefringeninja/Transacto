// ReSharper disable CheckNamespace

using System.Data.SQLite;
using Transacto.Plugins.FinancialStatements.BalanceSheet;

namespace Microsoft.Extensions.DependencyInjection;
// ReSharper restore CheckNamespace

static partial class ServiceCollectionExtensions {
	public static IServiceCollection AddBalanceSheet(this IServiceCollection services) => services
		.AddSQLiteProjection<BalanceSheetProjection>()
		.AddSingleton<GetBalanceSheetReportsAsync>(provider => {
			var connectionFactory =
				provider.GetRequiredKeyedService<Func<SQLiteConnection>>(typeof(BalanceSheetProjection));

			return async (thru, ct) => await Services.GetBalanceSheetReportsAsync(connectionFactory, thru, ct);
		});
}
