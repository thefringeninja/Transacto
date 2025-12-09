using System.Collections.Immutable;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Hallo;
using NodaTime;
using NodaTime.Text;
using Transacto.Domain;
using Transacto.Framework.Http;
using Transacto.Messages;
using Transacto.Plugins.FinancialStatements.BalanceSheet;

namespace Transacto.Integration;

public class BalanceSheetIntegrationTests(TextWriter textWriter) : IntegrationTests(textWriter) {
	[AutoFixtureData(1)]
	public async Task when_an_entry_is_posted(
		GeneralLedgerEntryIdentifier generalLedgerEntryIdentifier, LocalDateTime createdOn, Money subtotal,
		Money shipping, Money tax, Money inventory) {
		var period = AccountingPeriod.Open(createdOn.Date);
		var thru = createdOn.Date.PlusDays(1);
		var accounts = await OpenBooks(createdOn)
			.ToDictionaryAsync(a => a.AccountNumber.ToInt32(), a => a.AccountName.ToString());

		var invoice = new Invoice(1, subtotal, shipping, tax, inventory);

		var command = new PostGeneralLedgerEntry {
			GeneralLedgerEntryId = generalLedgerEntryIdentifier.ToGuid(),
			CreatedOn = createdOn.ToDateTimeUnspecified(),
			BusinessTransaction = invoice.ToJournalEntry(),
			Period = period.ToString()
		};


		// var expected = new BalanceSheetReports(accounts.ToImmutableDictionary(), [
		// 	ReportGroup.For(thru) with {
		// 		AccountBalances = [
		// 			new Line(
		// 				1100, (subtotal + tax + shipping).ToDecimal()),
		// 			new(1220, -inventory.ToDecimal()),
		// 			new(2300, tax.ToDecimal()),
		// 			new(4000, subtotal.ToDecimal()),
		// 			new(4050, shipping.ToDecimal()),
		// 			new(5000, inventory.ToDecimal())
		// 		]
		// 	}
		// ]);

		var position = await HttpClient.SendCommand("/general-ledger/entries", command,
			TransactoHttpSerializerOptions.BusinessTransactions(typeof(JournalEntry)));

		using var actual = await HttpClient.ConditionalGetAsync(
			$"/financial-statements/balance-sheet?thru={LocalDatePattern.Iso.Format(thru)}",
			position);

		await Assert.HalResponseEqual<BalanceSheetReports, BalanceSheetReportsRepresentation>(
			new(null!, []), actual);
	}

	private class Invoice(int number, Money subtotal, Money shipping, Money tax, Money inventory)
		: IBusinessTransaction {
		public GeneralLedgerEntrySequenceNumber SequenceNumber { get; } = new(number);

		public IEnumerable<object> GetTransactionItems() {
			var total = subtotal + shipping + tax;
			yield return new Debit(new AccountNumber(1100), total);
			yield return new Credit(new AccountNumber(1220), inventory);
			yield return new Credit(new AccountNumber(2300), tax);
			yield return new Credit(new AccountNumber(4000), subtotal);
			yield return new Credit(new AccountNumber(4050), shipping);
			yield return new Debit(new AccountNumber(5000), inventory);
		}
	}
}
