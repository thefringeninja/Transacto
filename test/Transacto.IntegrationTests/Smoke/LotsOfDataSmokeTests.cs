using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Hallo;
using NodaTime;
using Polly;
using Transacto.Domain;
using Transacto.Framework.Http;
using Transacto.Integration;
using Transacto.Messages;
using Transacto.Plugins.FinancialStatements;

namespace Transacto.Smoke;

public class LotsOfDataSmokeTests(TextWriter textWriter) : SmokeTests(textWriter) {
	private readonly TextWriter _textWriter = textWriter;

	public async Task one_year_of_invoices_and_payments() {
		var period = Period;

		var jsonSerializerOptions =
			TransactoHttpSerializerOptions.BusinessTransactions(typeof(JournalEntry));

		var entryIds = new List<Guid>();

		var openingBalanceId = Guid.NewGuid();

		await HttpClient.SendCommand("/general-ledger/entries", new PostGeneralLedgerEntry {
				BusinessTransaction = new JournalEntry {
					Description = "Opening Balance",
					Items = [
						new(3000, 1200.12m, JournalEntry.Type.Credit),
						new(1220, 1000m, JournalEntry.Type.Debit),
						new JournalEntry.Item(1040, 200.12m, JournalEntry.Type.Debit)
					]
				},
				CreatedOn = OpenDate,
				GeneralLedgerEntryId = openingBalanceId,
				Period = period.ToString()
			},
			jsonSerializerOptions);

		entryIds.Add(openingBalanceId);

		int sequenceNumber = 0;

		for (var month = 0; month < 12; month++) {
			int day = 0;
			var current = OpenDate.PlusMonths(month);
			var daysInMonth = CalendarSystem.Gregorian.GetDaysInMonth(current.Year, current.Month);
			for (var i = 0; i < 100; i++) {
				sequenceNumber++;
				if (sequenceNumber % 2 != 0) {
					var invoiceId = Guid.NewGuid();
					await HttpClient.SendCommand("/general-ledger/entries", new PostGeneralLedgerEntry {
							BusinessTransaction = new Invoice(new Money(100m), new Money(7.95m),
									Percentage.Of(8.25m), new Money(20m))
								.ToJournalEntry(ChartOfAccounts.Factory()),
							CreatedOn = current.PlusDays(day),
							GeneralLedgerEntryId = invoiceId,
							Period = period.ToString()
						},
						jsonSerializerOptions);
					entryIds.Add(invoiceId);
				} else {
					var paymentId = Guid.NewGuid();
					await HttpClient.SendCommand("/general-ledger/entries", new PostGeneralLedgerEntry {
							BusinessTransaction = new CreditCardPayment(new Money(108.25m + 7.95m), new Money(.5m),
									Percentage.Of(1.25m))
								.ToJournalEntry(ChartOfAccounts.Factory()),
							CreatedOn = current.PlusDays(day),
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
					ClosingOn = new LocalDate(current.Year, current.Month, daysInMonth),
					GeneralLedgerEntryIds = [.. entryIds],
					RetainedEarningsAccountNumber = 3030
				},
				jsonSerializerOptions);

			await _textWriter.WriteLineAsync($"Books closed {period}");

			period = period.Next();
			await Policy.HandleResult<string?>(s => !AccountingPeriod.TryParse(s, out var p) || p != period)
				.WaitAndRetryAsync(5, attempt => TimeSpan.FromMilliseconds(100 * Math.Pow(2, attempt)))
				.ExecuteAsync(async () => {
					using var response = await HttpClient.GetFromJsonAsync<JsonDocument>("/general-ledger/period");
					return response?.RootElement.GetProperty("period").GetString();
				});
			await _textWriter.WriteLineAsync($"Books opened {period}");
			entryIds.Clear();
		}

		var crawler = new HalCrawler(HttpClient, "/");

		await foreach (var link in crawler.CrawlAsync().Take(1024)) {
			using var request = new HttpRequestMessage(HttpMethod.Get, link) {
				Headers = { Accept = { new MediaTypeWithQualityHeaderValue("application/hal+html") } }
			};

			using var response = await HttpClient.SendAsync(request);

			Assert.InRange(response.StatusCode, HttpStatusCode.OK, (HttpStatusCode)399);
		}
	}

	private class Invoice(Money subtotal, Money shipping, Percentage salesTaxRate, Money inventory)
		: IBusinessTransaction {
		public GeneralLedgerEntrySequenceNumber? SequenceNumber { get; set; }
		public static GeneralLedgerEntryNumberPrefix Prefix { get; } = new("invoice");

		public GeneralLedgerEntryNumber? ReferenceNumber =>
			SequenceNumber.HasValue ? new GeneralLedgerEntryNumber(Prefix, SequenceNumber.Value) : null;

		public IEnumerable<object> GetTransactionItems(ChartOfAccounts chartOfAccounts) {
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

	private class CreditCardPayment(Money amount, Money flatFee, Percentage transactionFee)
		: IBusinessTransaction {
		public GeneralLedgerEntrySequenceNumber? SequenceNumber { get; set; }
		public static GeneralLedgerEntryNumberPrefix Prefix { get; } = new("payment");

		public GeneralLedgerEntryNumber? ReferenceNumber =>
			SequenceNumber.HasValue ? new GeneralLedgerEntryNumber(Prefix, SequenceNumber.Value) : null;

		public IEnumerable<object> GetTransactionItems(ChartOfAccounts chartOfAccounts) {
			var processingFee = (flatFee + amount * transactionFee).Round(2);
			var netPayment = amount - processingFee;
			yield return new Credit(new AccountNumber(1100), amount);
			yield return new Debit(new AccountNumber(6210), processingFee);
			yield return new Debit(new AccountNumber(1020), netPayment);
		}
	}
}
