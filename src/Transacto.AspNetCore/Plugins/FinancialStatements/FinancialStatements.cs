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
		builder.MapGet("/", () => {
			var startOfYear = LocalDate.FromDateTime(DateTime.Now);

			return Results.Hal(
				new FinancialStatementsIndex(startOfYear),
				Checkpoint.None,
				new FinancialStatementsIndexRepresentation());
		});
	}

	public void ConfigureServices(IServiceCollection services) => services
		.AddProfitAndLoss()
		.AddBalanceSheet()
		.AddCashFlow();
}