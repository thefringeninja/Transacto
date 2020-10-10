using System;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Transacto.Domain;
using Transacto.Messages;
using Transacto.Plugins.BalanceSheet;
using Xunit;

namespace Transacto.Integration {
	public class BalanceSheetIntegrationTests : IntegrationTests {
		[Theory, AutoTransactoData(1)]
		public async Task when_an_entry_is_posted(
			GeneralLedgerEntryIdentifier generalLedgerEntryIdentifier, DateTimeOffset createdOn, Money[] amounts) {
			var period = Period.Open(createdOn);
			var accounts = await OpenBooks(createdOn).ToArrayAsync();

			var debits = Array.ConvertAll(amounts, amount =>
				new Debit(accounts.OrderBy(_ => Guid.NewGuid()).First().accountNumber, amount));

			var credits = Array.ConvertAll(amounts, amount =>
				new Credit(accounts.OrderBy(_ => Guid.NewGuid()).First().accountNumber, amount));

			var command = new PostGeneralLedgerEntry {
				GeneralLedgerEntryId = generalLedgerEntryIdentifier.ToGuid(),
				CreatedOn = createdOn,
				BusinessTransaction = new JournalEntry {
					ReferenceNumber = 1,
					Credits = Array.ConvertAll(credits, credit => new JournalEntry.Item {
						Amount = credit.Amount.ToDecimal(),
						AccountNumber = credit.AccountNumber.Value
					}),
					Debits = Array.ConvertAll(debits, debit => new JournalEntry.Item {
						Amount = debit.Amount.ToDecimal(),
						AccountNumber = debit.AccountNumber.ToInt32()
					})
				},
				Period = period.ToString()
			};

			var position = await HttpClient.SendCommand("/general-ledger/entries", command,
				TransactoSerializerOptions.BusinessTransactions(typeof(JournalEntry)));

			using var response = await HttpClient.ConditionalGetAsync($"/balance-sheet/{createdOn.AddDays(1):O}", position);
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);
			var json = await response.Content.ReadAsStringAsync();
			var balanceSheet = JsonSerializer.Deserialize<BalanceSheetReport>(json, TransactoSerializerOptions.Events);
			Assert.Equal(accounts.Length, balanceSheet.LineItems.Count);
		}
	}
}
