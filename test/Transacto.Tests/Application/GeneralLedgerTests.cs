using System.Collections.Immutable;
using NodaTime;
using Transacto.Domain;
using Transacto.Messages;
using Transacto.Testing;

namespace Transacto.Application;

public class GeneralLedgerTests {
	private readonly GeneralLedgerHandlers _handler;
	private readonly IFactRecorder _facts;

	public GeneralLedgerTests() {
		_facts = new FactRecorder();
		_handler = new GeneralLedgerHandlers(new GeneralLedgerTestRepository(_facts),
			new ChartOfAccountsTestRepository(_facts));
	}

	[AutoFixtureData]
	public Task opening_the_period(LocalDate openedOn) =>
		new Scenario()
			.GivenNone()
			.When(new OpenGeneralLedger {
				OpenedOn = openedOn.ToDateTimeUnspecified()
			})
			.Then(GeneralLedger.Identifier, new GeneralLedgerOpened {
				OpenedOn = Time.Format.LocalDate(openedOn)
			})
			.Assert(_handler, _facts);

	[AutoFixtureData]
	public Task closing_an_open_period(YearMonth openedOn, EquityAccount retainedEarnings,
		GeneralLedgerEntryIdentifier[] generalLedgerEntryIdentifiers,
		GeneralLedgerEntryIdentifier closingGeneralLedgerEntryIdentifier) =>
		new Scenario()
			.Given(ChartOfAccounts.Identifier, new AccountDefined {
				AccountNumber = retainedEarnings.AccountNumber.ToInt32(),
				AccountName = retainedEarnings.AccountName.ToString()
			})
			.Given(GeneralLedger.Identifier, new GeneralLedgerOpened {
				OpenedOn = Time.Format.LocalDate(openedOn.OnDayOfMonth(1))
			})
			.When(new BeginClosingAccountingPeriod {
				GeneralLedgerEntryIds =
					ImmutableArray<Guid>.Empty.AddRange(Array.ConvertAll(generalLedgerEntryIdentifiers,
						x => x.ToGuid())),
				ClosingOn = openedOn.OnDayOfMonth(2).AtMidnight().ToDateTimeUnspecified(),
				RetainedEarningsAccountNumber = retainedEarnings.AccountNumber.ToInt32(),
				ClosingGeneralLedgerEntryId = closingGeneralLedgerEntryIdentifier.ToGuid()
			})
			.Then(GeneralLedger.Identifier, new AccountingPeriodClosing {
				Period = AccountingPeriod.Open(openedOn.OnDayOfMonth(1)).ToString(),
				GeneralLedgerEntryIds =
					Array.ConvertAll(generalLedgerEntryIdentifiers, x => x.ToGuid()).ToImmutableArray(),
				ClosingOn = Time.Format.LocalDateTime(openedOn.OnDayOfMonth(2).AtMidnight()),
				RetainedEarningsAccountNumber = retainedEarnings.AccountNumber.ToInt32(),
				ClosingGeneralLedgerEntryId = closingGeneralLedgerEntryIdentifier.ToGuid()
			})
			.Assert(_handler, _facts);

	[AutoFixtureData]
	public Task closing_a_closed_period(YearMonth openedOn, EquityAccount retainedEarnings,
		GeneralLedgerEntryIdentifier closingGeneralLedgerEntryIdentifier) =>
		new Scenario()
			.Given(ChartOfAccounts.Identifier, new AccountDefined {
				AccountNumber = retainedEarnings.AccountNumber.ToInt32(),
				AccountName = retainedEarnings.AccountName.ToString()
			})
			.Given(GeneralLedger.Identifier, new GeneralLedgerOpened {
					OpenedOn = Time.Format.LocalDate(openedOn.OnDayOfMonth(1))
				},
				new AccountingPeriodClosing {
					Period = AccountingPeriod.Open(openedOn.OnDayOfMonth(1)).ToString(),
					ClosingOn = Time.Format.LocalDateTime(openedOn.OnDayOfMonth(2).AtMidnight()),
					RetainedEarningsAccountNumber = retainedEarnings.AccountNumber.ToInt32(),
					ClosingGeneralLedgerEntryId = closingGeneralLedgerEntryIdentifier.ToGuid(),
					GeneralLedgerEntryIds = ImmutableArray<Guid>.Empty
				})
			.When(new BeginClosingAccountingPeriod {
				ClosingOn = openedOn.OnDayOfMonth(2).AtMidnight().ToDateTimeUnspecified(),
				RetainedEarningsAccountNumber = retainedEarnings.AccountNumber.ToInt32(),
				ClosingGeneralLedgerEntryId = closingGeneralLedgerEntryIdentifier.ToGuid(),
				GeneralLedgerEntryIds = ImmutableArray<Guid>.Empty
			})
			.Throws(new PeriodClosingInProcessException(AccountingPeriod.Open(openedOn.OnDayOfMonth(1))))
			.Assert(_handler, _facts);

	[AutoFixtureData]
	public Task closing_the_period_before_the_period_has_started(LocalDate openedOn, EquityAccount retainedEarnings,
		GeneralLedgerEntryIdentifier closingGeneralLedgerEntryIdentifier) =>
		new Scenario()
			.Given(ChartOfAccounts.Identifier, new AccountDefined {
				AccountNumber = retainedEarnings.AccountNumber.ToInt32(),
				AccountName = retainedEarnings.AccountName.ToString()
			})
			.Given(GeneralLedger.Identifier, new GeneralLedgerOpened {
				OpenedOn = Time.Format.LocalDate(openedOn)
			})
			.When(new BeginClosingAccountingPeriod {
				ClosingOn = openedOn.PlusMonths(-1).ToDateTimeUnspecified(),
				RetainedEarningsAccountNumber = retainedEarnings.AccountNumber.ToInt32(),
				ClosingGeneralLedgerEntryId = closingGeneralLedgerEntryIdentifier.ToGuid(),
				GeneralLedgerEntryIds = ImmutableArray<Guid>.Empty
			})
			.Throws(new ClosingDateBeforePeriodException(AccountingPeriod.Open(openedOn), openedOn.PlusMonths(-1)))
			.Assert(_handler, _facts);
}
