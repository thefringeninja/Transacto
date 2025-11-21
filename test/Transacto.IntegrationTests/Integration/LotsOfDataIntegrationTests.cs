using System.Net.Http.Json;
using System.Text.Json;
using NodaTime;
using Polly;
using Transacto.Domain;
using Transacto.Messages;

namespace Transacto.Integration;

public class LotsOfDataIntegrationTests : IntegrationTests {
	public async Task one_year_of_invoices_and_payments() {
		var openDate = new LocalDate(2025, 1, 1).AtMidnight();
		var period = AccountingPeriod.Open(openDate.Date);
		var accounts = await OpenBooks(openDate).ToArrayAsync();

		var jsonSerializerOptions =
			TransactoSerializerOptions.BusinessTransactions(typeof(JournalEntry));

		var entryIds = new List<Guid>();

		var openingBalanceId = Guid.NewGuid();

		await HttpClient.SendCommand("/general-ledger/entries", new PostGeneralLedgerEntry {
				BusinessTransaction = new JournalEntry {
					Description = "Opening Balance",
					JournalEntryNumber = 1,
					Items = [
						new(3000, 1200.12m, JournalEntry.Type.Credit),
						new(1220, 1000m, JournalEntry.Type.Debit),
						new JournalEntry.Item(1040, 200.12m, JournalEntry.Type.Debit)
					]
				},
				CreatedOn = openDate.ToDateTimeUnspecified(),
				GeneralLedgerEntryId = openingBalanceId,
				Period = period.ToString()
			},
			jsonSerializerOptions);

		entryIds.Add(openingBalanceId);

		int sequenceNumber = 0;

		for (var month = 0; month < 12; month++) {
			int day = 0;
			var current = openDate.PlusMonths(month);
			var daysInMonth = CalendarSystem.Gregorian.GetDaysInMonth(current.Year, current.Month);
			for (var i = 0; i < 1000; i++) {
				sequenceNumber++;
				if (sequenceNumber % 2 != 0) {
					var invoiceId = Guid.NewGuid();
					await HttpClient.SendCommand("/general-ledger/entries", new PostGeneralLedgerEntry {
							BusinessTransaction = new Invoice(sequenceNumber, new Money(100m), new Money(7.95m),
									Percentage.Of(8.25m), new Money(20m))
								.ToJournalEntry(),
							CreatedOn = current.ToDateTimeUnspecified().AddDays(day),
							GeneralLedgerEntryId = invoiceId,
							Period = period.ToString()
						},
						jsonSerializerOptions);
					entryIds.Add(invoiceId);
				} else {
					var paymentId = Guid.NewGuid();
					await HttpClient.SendCommand("/general-ledger/entries", new PostGeneralLedgerEntry {
							BusinessTransaction = new CreditCardPayment(
									sequenceNumber, new Money(108.25m + 7.95m), new Money(.5m), Percentage.Of(1.25m))
								.ToJournalEntry(),
							CreatedOn = current.ToDateTimeUnspecified().AddDays(day),
							GeneralLedgerEntryId = paymentId,
							Period = period.ToString()
						},
						jsonSerializerOptions);
					entryIds.Add(paymentId);
					day++;
					day %= daysInMonth;
				}
			}

			var closingGeneralLedgerEntryId = Guid.NewGuid();
			await HttpClient.SendCommand("/general-ledger", new BeginClosingAccountingPeriod {
					ClosingGeneralLedgerEntryId = closingGeneralLedgerEntryId,
					ClosingOn = new DateTimeOffset(new DateOnly(current.Year, current.Month, daysInMonth),
						TimeOnly.MinValue, TimeSpan.Zero),
					GeneralLedgerEntryIds = [..entryIds],
					RetainedEarningsAccountNumber = 3030
				},
				jsonSerializerOptions);

			period = period.Next();
			await Policy.HandleResult<string?>(s => {
					if (!AccountingPeriod.TryParse(s, out var p)) return true;
					return p != period;
				})
				.RetryForeverAsync()
				.ExecuteAsync(async () => {
					var response = await HttpClient.GetFromJsonAsync<JsonDocument>("/general-ledger/period");
					return response?.RootElement.GetProperty("period").GetString();
				});
			entryIds.Clear();
		}
	}

	private class Invoice(int number, Money subtotal, Money shipping, Percentage salesTaxRate, Money inventory)
		: IBusinessTransaction {
		public GeneralLedgerEntrySequenceNumber SequenceNumber { get; } = new(number);

		public IEnumerable<object> GetTransactionItems() {
			var tax = subtotal * salesTaxRate;
			var total = subtotal + shipping + tax;
			yield return new Debit(new AccountNumber(1100), total);
			yield return new Credit(new AccountNumber(1220), inventory);
			yield return new Credit(new AccountNumber(2310), tax);
			yield return new Credit(new AccountNumber(4000), subtotal);
			yield return new Credit(new AccountNumber(4050), shipping);
			yield return new Debit(new AccountNumber(5000), inventory);
		}
	}

	private class CreditCardPayment(int number, Money amount, Money flatFee, Percentage transactionFee)
		: IBusinessTransaction {
		public GeneralLedgerEntrySequenceNumber SequenceNumber { get; } = new(number);

		public IEnumerable<object> GetTransactionItems() {
			var processingFee = (flatFee + amount * transactionFee).Round(2);
			var netPayment = amount - processingFee;
			yield return new Credit(new AccountNumber(1100), amount);
			yield return new Debit(new AccountNumber(6210), processingFee);
			yield return new Debit(new AccountNumber(1020), netPayment);
		}
	}
}
