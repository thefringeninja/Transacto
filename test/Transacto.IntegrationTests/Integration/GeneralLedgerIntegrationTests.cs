using NodaTime;
using Transacto.Domain;
using Transacto.HttpSpecifications;

namespace Transacto.Integration;

public class GeneralLedgerIntegrationTests(TextWriter logOutput) : IntegrationTests(logOutput) {
	[AutoFixtureData(1)]
	public Task period(LocalDate openedOn) => new GeneralLedgerHttpSpecifications.Period(openedOn).Assert(HttpClient);

	[AutoFixtureData(1)]
	public Task entries(GeneralLedgerEntryIdentifier[] generalLedgerEntryIdentifiers, LocalDate openedOn) =>
		new GeneralLedgerHttpSpecifications.SearchEntries(generalLedgerEntryIdentifiers, openedOn)
			.Assert(HttpClient);
}
