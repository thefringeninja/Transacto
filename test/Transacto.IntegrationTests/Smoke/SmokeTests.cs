using NodaTime;
using Transacto.Domain;
using Transacto.Integration;

namespace Transacto.Smoke;

public abstract class SmokeTests : IntegrationTests {
	protected LocalDateTime OpenDate { get; }
	protected AccountingPeriod Period { get; }

	protected Account[] Accounts { get; private set; } = [];


	protected SmokeTests(TextWriter logOutput) : base(logOutput) {
		OpenDate = new LocalDate(2025, 1, 1).AtMidnight();
		Period = AccountingPeriod.Open(OpenDate.Date);
	}

	public async Task InitializeAsync() {
		Accounts = await OpenBooks(OpenDate).ToArrayAsync();
	}
}
