using NodaTime;
using Transacto.Domain;
using Transacto.HttpSpecifications;

namespace Transacto.Integration;

public class BalanceSheetIntegrationTests(TextWriter textWriter) : IntegrationTests(textWriter) {
	[AutoFixtureData(1)]
	public Task when_books_are_opened(LocalDate openedOn) =>
		new BalanceSheetHttpSpecifications.OpeningTheBooks(openedOn).Assert(HttpClient);

	[AutoFixtureData(1)]
	public Task when_an_entry_is_posted(
		GeneralLedgerEntryIdentifier generalLedgerEntryIdentifier, LocalDate createdOn, Money subtotal,
		Money shipping, Money tax, Money inventory) =>
		new BalanceSheetHttpSpecifications.WhenAnEntryIsPosted(generalLedgerEntryIdentifier, createdOn, subtotal,
			shipping, tax, inventory).Assert(HttpClient);
}
