using System.Net;
using System.Text.Json;
using Transacto.Domain;
using Transacto.Framework;
using Transacto.Framework.Http;
using Transacto.Messages;

namespace Transacto.Integration;

public class ChartOfAccountsIntegrationTests(TextWriter textWriter) : IntegrationTests(textWriter) {
	public async Task creating_a_chart_of_accounts() {
		var checkpoint = await SetupChartOfAccounts();

		var position = checkpoint.ToEventStoreCheckpoint();

		using var response = await HttpClient.ConditionalGetAsync("/chart-of-accounts", checkpoint);

		var body = await response.Content.ReadAsStreamAsync();
		var chartOfAccounts = await JsonDocument.ParseAsync(body);

		var actual = chartOfAccounts.RootElement
			.EnumerateObject()
			.Where(x => x.Name != "_links" && x.Name != "_embedded")
			.Select(x => new {
				accountName = x.Value.GetString()!,
				accountNumber = int.Parse(x.Name)
			}).OrderBy(x => x.accountNumber);
		var expected = ChartOfAccounts
			.Select(x => new {
				accountName = x.AccountName.ToString(),
				accountNumber = x.AccountNumber.ToInt32()
			})
			.OrderBy(x => x.accountNumber);

		Assert.Equal(expected, actual);

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
	}

	private async Task<Checkpoint> SetupChartOfAccounts() {
		Checkpoint checkpoint = Checkpoint.None;

		await HttpClient.SendCommand("/chart-of-accounts", new ConfigureChartOfAccounts {
			AccountNumberWidth = 4
		}, TransactoHttpSerializerOptions.Commands);
		foreach (var account in ChartOfAccounts.OrderBy(_ => Guid.NewGuid())) {
			checkpoint = await HttpClient.SendCommand("/chart-of-accounts", new DefineAccount {
				AccountName = account.AccountName.ToString(),
				AccountNumber = account.AccountNumber.ToInt32(),
				CashFlowCategory = account.CashFlowCategory.ToEnum(),
				AffectsCash = account.AffectsCash
			}, TransactoHttpSerializerOptions.Commands);
		}

		return checkpoint;
	}

	private static IEnumerable<Account> ChartOfAccounts {
		get {
			yield return Account.For(new AccountNumber(1000), new AccountName("Bank Checking Account"),
				CashFlowCategory.Operations);
			yield return Account.For(new AccountNumber(1050), new AccountName("Bank Savings Account"),
				CashFlowCategory.Operations);
			yield return Account.For(new AccountNumber(1200), new AccountName("Accounts Receivable"),
				CashFlowCategory.Operations);
			yield return Account.For(new AccountNumber(2000), new AccountName("Accounts Payable"),
				CashFlowCategory.Operations);
			yield return Account.For(new AccountNumber(3000), new AccountName("Opening Balance Equity"),
				CashFlowCategory.Finance);
			yield return Account.For(new AccountNumber(3900), new AccountName("Retained Earnings"),
				CashFlowCategory.Finance);
			yield return Account.For(new AccountNumber(4000), new AccountName("Sales Income"),
				CashFlowCategory.Operations);
			yield return Account.For(new AccountNumber(5000), new AccountName("Cost of Goods Sold"),
				CashFlowCategory.Operations);
		}
	}
}
