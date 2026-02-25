using NodaTime;
using Transacto.Domain;
using Transacto.Testing;
using static Transacto.Specifications.GeneralLedgerSpecifications;

namespace Transacto.Application;

public class GeneralLedgerTests {
	private readonly IFactRecorder _facts;
	private readonly GeneralLedgerHandlers _handler;

	public GeneralLedgerTests() {
		_facts = new FactRecorder();
		_handler = new GeneralLedgerHandlers(new GeneralLedgerTestRepository(_facts),
			new ChartOfAccountsTestRepository(_facts));
	}

	[AutoFixtureData]
	public Task opening_the_period(LocalDate openedOn) => new OpeningThePeriod(openedOn).Assert(_handler, _facts);

	[AutoFixtureData]
	public Task closing_an_open_period(YearMonth openedOn,
		EquityAccount retainedEarnings,
		AssetAccount accountsReceivable,
		LiabilityAccount accountsPayable,
		GeneralLedgerEntryIdentifier[] generalLedgerEntryIdentifiers,
		GeneralLedgerEntryIdentifier closingGeneralLedgerEntryIdentifier) =>
		new ClosingAnOpenPeriod(openedOn, retainedEarnings, accountsReceivable, accountsPayable,
				generalLedgerEntryIdentifiers,
				closingGeneralLedgerEntryIdentifier)
			.Assert(_handler, _facts);

	[AutoFixtureData]
	public Task closing_a_closed_period(YearMonth openedOn, EquityAccount retainedEarnings,
		AssetAccount accountsReceivable,
		LiabilityAccount accountsPayable,
		GeneralLedgerEntryIdentifier closingGeneralLedgerEntryIdentifier) =>
		new ClosingAClosedPeriod(openedOn, retainedEarnings, accountsReceivable, accountsPayable,
				closingGeneralLedgerEntryIdentifier)
			.Assert(_handler, _facts);

	[AutoFixtureData]
	public Task closing_the_period_before_the_period_has_started(LocalDate openedOn, 
		EquityAccount retainedEarnings,
		AssetAccount accountsReceivable,
		LiabilityAccount accountsPayable,
		GeneralLedgerEntryIdentifier closingGeneralLedgerEntryIdentifier) =>
		new ClosingThePeriodBeforeThePeriodHasStarted(openedOn, retainedEarnings,
			accountsReceivable, accountsPayable, closingGeneralLedgerEntryIdentifier).Assert(_handler, _facts);
}
