using NodaTime;
using Transacto.Domain;
using Transacto.Integration;

namespace Transacto.Smoke;

public abstract class SmokeTests : IntegrationTests {
	protected LocalDate OpenDate { get; }
	protected AccountingPeriod Period { get; }

	protected Account[] Accounts { get; private set; } = [];


	protected SmokeTests(TextWriter logOutput) : base(logOutput) {
		OpenDate = new LocalDate(2025, 1, 1);
		Period = AccountingPeriod.Open(OpenDate);
	}

	public async Task InitializeAsync() {
		Accounts = await OpenBooks(OpenDate).ToArrayAsync();
	}
}
