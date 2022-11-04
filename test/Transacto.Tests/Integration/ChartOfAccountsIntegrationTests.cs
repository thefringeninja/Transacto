using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Transacto.Domain;
using Transacto.Framework;
using Transacto.Messages;
using Xunit;

namespace Transacto.Integration; 

public class ChartOfAccountsIntegrationTests : IntegrationTests {
	[Fact]
	public async Task hal() {
		var checkpoint = await SetupChartOfAccounts();

		var position = checkpoint.ToEventStorePosition();

		using var response = await HttpClient.ConditionalGetAsync("/chart-of-accounts", checkpoint);

		var body = await response.Content.ReadAsStreamAsync();
		var chartOfAccounts = await JsonDocument.ParseAsync(body);

		var actual = chartOfAccounts.RootElement
			.EnumerateObject()
			.Where(x => x.Name != "_links" && x.Name != "_embedded")
			.Select(x => new {
				accountName = x.Value.GetString()!,
				accountNumber = int.Parse(x.Name)
			});
		var expected = ChartOfAccounts
			.OrderBy(x => x.accountNumber.ToInt32())
			.Select(x => new {
				accountName = x.accountName.ToString(),
				accountNumber = x.accountNumber.ToInt32()
			});

		Assert.Equal(expected, actual);

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
	}

	private async Task<Checkpoint> SetupChartOfAccounts() {
		Checkpoint checkpoint = Checkpoint.None;

		foreach (var (accountNumber, accountName) in ChartOfAccounts.OrderBy(_ => Guid.NewGuid())) {
			checkpoint = await HttpClient.SendCommand("/chart-of-accounts", new DefineAccount {
				AccountName = accountName.ToString(),
				AccountNumber = accountNumber.ToInt32()
			}, TransactoSerializerOptions.Commands);
		}

		return checkpoint;
	}

	private static IEnumerable<(AccountNumber accountNumber, AccountName accountName)> ChartOfAccounts {
		get {
			yield return (new AccountNumber(1000), new AccountName("Bank Checking Account"));
			yield return (new AccountNumber(1050), new AccountName("Bank Savings Account"));
			yield return (new AccountNumber(1200), new AccountName("Accounts Receivable"));
			yield return (new AccountNumber(2000), new AccountName("Accounts Payable"));
			yield return (new AccountNumber(3000), new AccountName("Opening Balance Equity"));
			yield return (new AccountNumber(3900), new AccountName("Retained Earnings"));
			yield return (new AccountNumber(4000), new AccountName("Sales Income"));
			yield return (new AccountNumber(5000), new AccountName("Cost of Goods Sold"));
		}
	}
}