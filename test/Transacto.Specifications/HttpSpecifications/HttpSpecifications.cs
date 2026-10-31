using System.Text.Json;
using NodaTime;
using Transacto.Domain;
using Transacto.Messages;
using Transacto.Testing.Http;

namespace Transacto.HttpSpecifications;

public abstract class HttpSpecifications {
	private static readonly Account[] Accounts = [.. ReadAccounts()];

	protected static readonly HttpRequest[] DefineChartOfAccounts = [
		HttpRequest.Command(new ConfigureChartOfAccounts {
			AccountNumberWidth = 4,
			AccountsReceivable = 1100,
			AccountsPayable = 2000,
			RetainedEarnings = 3030
		}, "/chart-of-accounts"),
		.. Accounts.Select(account =>
			HttpRequest.Command(new DefineAccount {
				AccountName = account.AccountName.ToString(),
				AccountNumber = account.AccountNumber.ToInt32(),
				CashFlowCategory = account.CashFlowCategory.ToEnum(),
				AffectsCash = account.AffectsCash
			}, "/chart-of-accounts"))
	];

	protected static HttpRequest OpenBooks(LocalDate openedOn) => HttpRequest.Command(new OpenGeneralLedger {
		OpenedOn = openedOn
	}, "/general-ledger");

	private static IEnumerable<Account> ReadAccounts() {
		using var stream =
			typeof(HttpSpecification).Assembly.GetManifestResourceStream(typeof(HttpSpecification),
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
}
