// ReSharper disable CheckNamespace

using Microsoft.AspNetCore.Mvc;
using NodaTime;
using Transacto.Framework;
using Transacto.Framework.Http;
using Transacto.Plugins.FinancialStatements.BalanceSheet;

namespace Microsoft.AspNetCore.Builder;
// ReSharper restore CheckNamespace

static partial class ApplicationBuilderExtensions {
	public static IEndpointRouteBuilder UseBalanceSheet(this IEndpointRouteBuilder builder) {
		builder.MapGet("", async (
			[FromQuery] ParseableLocalDate?[] thru,
			[FromServices] GetBalanceSheetReportsAsync getBalanceSheetReportsAsync,
			[FromServices] BalanceSheetReportsRepresentation representation,
			CancellationToken ct) => {
			var resource =
				await getBalanceSheetReportsAsync([..Array.ConvertAll(thru, t => t ?? LocalDate.MaxIsoValue)], ct);

			return Results.Hal(resource, Checkpoint.None, representation);
		});
		return builder;
	}

}
