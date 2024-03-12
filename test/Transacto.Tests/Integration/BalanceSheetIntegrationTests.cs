using System.Collections.Immutable;
using System.Net;
using System.Text.Json;
using NodaTime;
using Transacto.Domain;
using Transacto.Messages;
using Transacto.Plugins.BalanceSheet;
using Decimal = Transacto.Plugins.BalanceSheet.Decimal;

namespace Transacto.Integration;

public class BalanceSheetIntegrationTests : IntegrationTests {
	[AutoFixtureData(1)]
	private async Task when_an_entry_is_posted(
		GeneralLedgerEntryIdentifier generalLedgerEntryIdentifier, LocalDateTime createdOn, Money[] amounts) {
		var period = AccountingPeriod.Open(createdOn.Date);
		var thru = createdOn.Date.PlusDays(1).AtMidnight();
		var accounts = await OpenBooks(createdOn).ToArrayAsync();

		var debits = Array.ConvertAll(amounts, amount =>
			new Debit(accounts.MinBy(_ => Guid.NewGuid()).accountNumber, amount));

		var credits = Array.ConvertAll(amounts, amount =>
			new Credit(accounts.MinBy(_ => Guid.NewGuid()).accountNumber, amount));

		var journalEntry = new JournalEntry {
			JournalEntryNumber = 1,
			Items = credits.Select(credit => new JournalEntry.Item(credit.AccountNumber.ToInt32(),
					credit.Amount.ToDecimal(), JournalEntry.Type.Credit))
				.Concat(debits.Select(debit => new JournalEntry.Item(debit.AccountNumber.ToInt32(),
					debit.Amount.ToDecimal(), JournalEntry.Type.Debit)))
				.ToImmutableArray()
		};

		var command = new PostGeneralLedgerEntry {
			GeneralLedgerEntryId = generalLedgerEntryIdentifier.ToGuid(),
			CreatedOn = createdOn.ToDateTimeUnspecified(),
			BusinessTransaction = journalEntry,
			Period = period.ToString()
		};

		var expected = new BalanceSheetReport {
			Thru = thru.ToDateTimeUnspecified(),
			LineItems = journalEntry.Items.Aggregate(ImmutableDictionary<int, LineItem>.Empty, (items, item) =>
				items.SetItem(
					item.AccountNumber,
					new LineItem {
						AccountNumber = item.AccountNumber,
						Name = accounts.Single(x => x.accountNumber.ToInt32() == item.AccountNumber).accountName
							.ToString(),
						Balance = items.TryGetValue(item.AccountNumber, out var existing)
							? existing.Balance + (item.Type == JournalEntry.Type.Debit
								? item.Amount
								: -item.Amount)
							: Decimal.Create(item.Type == JournalEntry.Type.Debit
								? item.Amount
								: -item.Amount)
					})).Values.OrderBy(x => x.AccountNumber).ToImmutableArray(),
			LineItemGroupings = ImmutableArray<LineItemGrouping>.Empty
		};

		var position = await HttpClient.SendCommand("/general-ledger/entries", command,
			TransactoSerializerOptions.BusinessTransactions(typeof(JournalEntry)));

		using var response =
			await HttpClient.ConditionalGetAsync($"/balance-sheet/{Time.Format.LocalDateTime(thru)}",
				position);
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		var json = await response.Content.ReadAsStringAsync();
		var actual = JsonSerializer.Deserialize<BalanceSheetReport>(json, TransactoSerializerOptions.Events)!;

		Assert.Equal(expected, actual);
	}
}
