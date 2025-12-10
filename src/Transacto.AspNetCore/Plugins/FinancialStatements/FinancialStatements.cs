using Microsoft.AspNetCore.Mvc;
using NodaTime;
using Transacto.Framework;
using Transacto.Framework.Http;

namespace Transacto.Plugins.FinancialStatements;

public class FinancialStatements : IPlugin {
	public string Name => nameof(FinancialStatements);
	public IEnumerable<Type> MessageTypes { get; } = [];

	public void Configure(IEndpointRouteBuilder builder) {
		builder.MapGroup("/profit-and-loss").UseProfitAndLoss();
		builder.MapGroup("/balance-sheet").UseBalanceSheet();
		builder.MapGroup("/cash-flow").UseCashFlow();
		builder.MapGet("/", ([FromServices] FinancialStatementsIndexRepresentation representation) => {
			var now = LocalDate.FromDateTime(DateTime.Now);
			
			var startOfYear = now.PlusDays(-now.DayOfYear+1);

			return Results.Hal(
				new FinancialStatementsIndex(startOfYear),
				Checkpoint.None,
				representation);
		});
	}

	public void ConfigureServices(IServiceCollection services) => services
		.AddProfitAndLoss()
		.AddBalanceSheet()
		.AddCashFlow()
		.AddSingleton<FinancialStatementsIndexRepresentation>();
}
