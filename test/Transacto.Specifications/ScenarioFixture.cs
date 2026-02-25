using System.Text.Json;
using AutoFixture;
using AutoFixture.Kernel;
using Bogus;
using Transacto.Domain;

namespace Transacto;

internal static class RealisticFixtureExtensions {
	private static IEnumerable<Account> GetChartOfAccounts() {
		using var stream =
			typeof(RealisticFixtureExtensions).Assembly.GetManifestResourceStream(typeof(RealisticFixtureExtensions),
				"ChartOfAccounts.json") ??
			throw new InvalidOperationException();

		var doc = JsonDocument.Parse(stream);

		foreach (var account in doc.RootElement.EnumerateArray()) {
			yield return Account.For(
				new(account.GetProperty("accountNumber").GetInt32()),
				new(account.GetProperty("accountName").GetString()!),
				account.GetProperty("cashFlowCategory").GetString() switch {
					"operations" => CashFlowCategory.Operations,
					"investment" => CashFlowCategory.Investment,
					"finance" => CashFlowCategory.Finance,
					_ => CashFlowCategory.None
				},
				account.TryGetProperty("affectsCash", out var affectsCash) && affectsCash.GetBoolean());
		}
	}

	private static Account[] Accounts { get; } = GetChartOfAccounts().ToArray();


	private static Dictionary<int, Account[]> AccountsByType { get; } =
		Accounts.ToLookup(a => int.Parse([a.AccountNumber.ToString()[0]]))
			.ToDictionary(g => g.Key, g => g.ToArray());

	public static void CustomizeRealisticAccountName(this Fixture fixture, Faker faker) =>
		fixture.Customize<AccountName>(composer =>
			composer.FromFactory(() => new AccountName(faker.Finance.RoutingNumber())));

	public static void CustomizeRealisticAccount(this Fixture fixture, Faker faker) {
		fixture.CustomizeRealisticAccount<AssetAccount>(1, faker);
		fixture.CustomizeRealisticAccount<LiabilityAccount>(2, faker);
		fixture.CustomizeRealisticAccount<EquityAccount>(3, faker);
		fixture.CustomizeRealisticAccount<IncomeAccount>(4, faker);
		fixture.CustomizeRealisticAccount<CostOfRevenueExpenseAccount>(5, faker);
		fixture.CustomizeRealisticAccount<OperatingExpenseAccount>(6, faker);
		fixture.CustomizeRealisticAccount<OtherIncomeAccount>(7, faker);
		fixture.CustomizeRealisticAccount<OtherExpenseAccount>(8, faker);
		fixture.Customizations.Add(new TypeRelay(typeof(ExpenseAccount), typeof(CostOfRevenueExpenseAccount)));
		fixture.Customizations.Add(new TypeRelay(typeof(Account), typeof(AssetAccount)));
	}

	private static void CustomizeRealisticAccount<TAccount>(this Fixture fixture, int type, Faker faker)
		where TAccount : Account =>
		fixture.Customize<TAccount>(composer =>
			composer.FromFactory(() =>
				(TAccount)AccountsByType[type][faker.Random.Int(0, AccountsByType[type].Length - 1)]));
}

partial class ScenarioFixture : Fixture {
	public ScenarioFixture(Randomizer? randomizer = null) {
		randomizer ??= new Randomizer();
		var random = new Random(randomizer.Number());

		var faker = new Faker {
			Random = randomizer
		};

		this.CustomizeNodaTime();

		this.CustomizeAccountingPeriod();

		this.CustomizeRealisticAccountName(faker);

		this.CustomizeAccountNumber();

		this.Customize<AccountNumberWidth>(composer => composer.FromFactory(() => AccountNumberWidth.Min));

		this.CustomizeRealisticAccount(faker);

		this.CustomizeGeneralLedgerEntryNumberPrefix();

		this.CustomizeMoney();

		this.CustomizeCredit();

		this.CustomizeDebit();

		this.CustomizeCashFlowCategory();


		Customizations.Add(new Int32SeededGenerator(random));
		Customizations.Add(new SeededDecimalGenerator(random));
	}

	private class Int32SeededGenerator(Random random) : ISpecimenBuilder {
		public object Create(object request, ISpecimenContext context) =>
			typeof(int).Equals(request) ? random.Next() : new NoSpecimen();
	}

	private class SeededDecimalGenerator(Random random) : ISpecimenBuilder {
		public object Create(object request, ISpecimenContext context) =>
			typeof(decimal).Equals(request) ? (random.Next() / 100m) : new NoSpecimen();
	}
}
