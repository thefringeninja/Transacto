using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using EventStore.Client;
using Transacto.Domain;
using Transacto.Messages;
using Xunit;

namespace Transacto.Integration {
	public class ChartOfAccountsIntegrationTests : IntegrationTests {
		[Fact]
		public async Task html() {
		}

		[Fact]
		public async Task hal() {
			var position = await SetupChartOfAccounts();

			using var response = await HttpClient.ConditionalGetAsync("/chart-of-accounts", position);

			var body = await response.Content.ReadAsStreamAsync();
			var chartOfAccounts = await JsonDocument.ParseAsync(body);

			using var resultEnumerator = chartOfAccounts.RootElement.EnumerateObject()
				.Where(x => x.Name != "_links" && x.Name != "_embedded")
				.GetEnumerator();
			using var expectEnumerator = ChartOfAccounts.OrderBy(x => x.Item1.ToInt32()).GetEnumerator();

			while (expectEnumerator.MoveNext() && resultEnumerator.MoveNext()) {
				Assert.Equal(expectEnumerator.Current.Item1.ToString(), resultEnumerator.Current.Name);
				Assert.Equal(expectEnumerator.Current.Item2.ToString(), resultEnumerator.Current.Value.ToString());
			}

			Assert.False(expectEnumerator.MoveNext());
			Assert.False(resultEnumerator.MoveNext());

			Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		}

		private async Task<Position> SetupChartOfAccounts()
		{
			Position position = Position.Start;

			foreach (var (accountNumber, accountName) in ChartOfAccounts.OrderBy(_ => Guid.NewGuid()))
			{
				position = await HttpClient.SendCommand("/chart-of-accounts", new DefineAccount
				{
					AccountName = accountName.ToString(),
					AccountNumber = accountNumber.ToInt32()
				}, TransactoSerializerOptions.Commands);
			}

			return position;
		}

		private static IEnumerable<(AccountNumber, AccountName)> ChartOfAccounts {
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
}
