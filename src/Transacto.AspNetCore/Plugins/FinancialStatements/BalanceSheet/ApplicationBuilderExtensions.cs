// ReSharper disable CheckNamespace

using Microsoft.AspNetCore.Mvc;
using NodaTime;
using Transacto.Framework;
using Transacto.Framework.Http;
using Transacto.Plugins.FinancialStatements;
using Transacto.Plugins.FinancialStatements.BalanceSheet;
using Transacto.Plugins.GeneralLedger;

namespace Microsoft.AspNetCore.Builder;
// ReSharper restore CheckNamespace

static partial class ApplicationBuilderExtensions {
	public static IEndpointRouteBuilder UseBalanceSheet(this IEndpointRouteBuilder builder) {
		var cachePolicy = CachePolicies.Thru(builder.ServiceProvider.GetRequiredService<GetCurrentPeriodAsync>()); 
		builder.MapGet("", async (
			[FromQuery] ParseableLocalDate?[] thru,
			[FromServices] GetBalanceSheetReportsAsync getBalanceSheetReportsAsync,
			[FromServices] BalanceSheetReportsRepresentation representation,
			CancellationToken ct) => {
			var resource =
				await getBalanceSheetReportsAsync([..Array.ConvertAll(thru, t => t ?? LocalDate.MaxIsoValue)], ct);

			return Results.Hal(resource, Checkpoint.None, representation);
		}).CacheControl(cachePolicy);
		return builder;
	}
}
