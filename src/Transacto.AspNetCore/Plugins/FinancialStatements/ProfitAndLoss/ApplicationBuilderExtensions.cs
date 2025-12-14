// ReSharper disable CheckNamespace

using Microsoft.AspNetCore.Mvc;
using NodaTime;
using Transacto.Framework;
using Transacto.Framework.Http;
using Transacto.Plugins.FinancialStatements;
using Transacto.Plugins.FinancialStatements.ProfitAndLoss;
using Transacto.Plugins.GeneralLedger;

namespace Microsoft.AspNetCore.Builder;
// ReSharper restore CheckNamespace

static partial class ApplicationBuilderExtensions {
	public static IEndpointRouteBuilder UseProfitAndLoss(this IEndpointRouteBuilder builder) {
		var cachePolicy = CachePolicies.Range(builder.ServiceProvider.GetRequiredService<GetCurrentPeriodAsync>()); 
		builder.MapGet("",
			async ([FromQuery(Name = "starting-on")] ParseableLocalDate[] startingOn,
				[FromQuery] ParseablePeriod[] period,
				[FromServices] GetProfitAndLossReportsAsync getProfitAndLossReportsAsync,
				[FromServices] ProfitAndLossReportsRepresentation representation,
				CancellationToken ct) => {
				var reportRanges = new (LocalDate, Period)[Math.Min(startingOn.Length, period.Length)];

				for (var i = 0; i < reportRanges.Length; i++) {
					reportRanges[i] = (startingOn[i], period[i]);
				}

				var resource = await getProfitAndLossReportsAsync([..reportRanges], ct);

				return Results.Hal(resource, Checkpoint.None, representation);
			}).CacheControl(cachePolicy);
		return builder;
	}
}
