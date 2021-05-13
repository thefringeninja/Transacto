using System;
using System.Threading.Tasks;
using NodaTime;
using Transacto.Domain;
using Transacto.Messages;
using Transacto.Testing;
using Xunit;

namespace Transacto.Application {
	public class GeneralLedgerTests {
		private readonly GeneralLedgerHandlers _handler;
		private readonly IFactRecorder _facts;
		private readonly AccountNumber _retainedEarnings;

		public GeneralLedgerTests() {
			_facts = new FactRecorder();
			_handler = new GeneralLedgerHandlers(
				new GeneralLedgerTestRepository(_facts));
			_retainedEarnings = new AccountNumber(new Random().Next(3000, 3999));
		}

		[Theory, AutoTransactoData]
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

		[Theory, AutoTransactoData]
		public Task closing_an_open_period(YearMonth openedOn,
			GeneralLedgerEntryIdentifier[] generalLedgerEntryIdentifiers,
			GeneralLedgerEntryIdentifier closingGeneralLedgerEntryIdentifier) =>
			new Scenario()
				.Given(GeneralLedger.Identifier, new GeneralLedgerOpened {
					OpenedOn = Time.Format.LocalDate(openedOn.OnDayOfMonth(1))
				})
				.When(new BeginClosingAccountingPeriod {
					GeneralLedgerEntryIds = Array.ConvertAll(generalLedgerEntryIdentifiers, x => x.ToGuid()),
					ClosingOn = openedOn.OnDayOfMonth(2).AtMidnight().ToDateTimeUnspecified(),
					RetainedEarningsAccountNumber = _retainedEarnings.ToInt32(),
					ClosingGeneralLedgerEntryId = closingGeneralLedgerEntryIdentifier.ToGuid()
				})
				.Then(GeneralLedger.Identifier, new AccountingPeriodClosing {
					Period = AccountingPeriod.Open(openedOn.OnDayOfMonth(1)).ToString(),
					GeneralLedgerEntryIds = Array.ConvertAll(generalLedgerEntryIdentifiers, x => x.ToGuid()),
					ClosingOn = Time.Format.LocalDateTime(openedOn.OnDayOfMonth(2).AtMidnight()),
					RetainedEarningsAccountNumber = _retainedEarnings.ToInt32(),
					ClosingGeneralLedgerEntryId = closingGeneralLedgerEntryIdentifier.ToGuid()
				})
				.Assert(_handler, _facts);

		[Theory, AutoTransactoData]
		public Task closing_a_closed_period(YearMonth openedOn,
			GeneralLedgerEntryIdentifier closingGeneralLedgerEntryIdentifier) =>
			new Scenario()
				.Given(GeneralLedger.Identifier, new GeneralLedgerOpened {
						OpenedOn = Time.Format.LocalDate(openedOn.OnDayOfMonth(1))
					},
					new AccountingPeriodClosing {
						Period = AccountingPeriod.Open(openedOn.OnDayOfMonth(1)).ToString(),
						ClosingOn = Time.Format.LocalDateTime(openedOn.OnDayOfMonth(2).AtMidnight()),
						RetainedEarningsAccountNumber = _retainedEarnings.ToInt32(),
						ClosingGeneralLedgerEntryId = closingGeneralLedgerEntryIdentifier.ToGuid()
					})
				.When(new BeginClosingAccountingPeriod {
					ClosingOn = openedOn.OnDayOfMonth(2).AtMidnight().ToDateTimeUnspecified(),
					RetainedEarningsAccountNumber = _retainedEarnings.ToInt32(),
					ClosingGeneralLedgerEntryId = closingGeneralLedgerEntryIdentifier.ToGuid()
				})
				.Throws(new PeriodClosingInProcessException(AccountingPeriod.Open(openedOn.OnDayOfMonth(1))))
				.Assert(_handler, _facts);

		[Theory, AutoTransactoData]
		public Task closing_the_period_before_the_period_has_started(LocalDate openedOn,
			GeneralLedgerEntryIdentifier closingGeneralLedgerEntryIdentifier) =>
			new Scenario()
				.Given(GeneralLedger.Identifier, new GeneralLedgerOpened {
					OpenedOn = Time.Format.LocalDate(openedOn)
				})
				.When(new BeginClosingAccountingPeriod {
					ClosingOn = openedOn.PlusMonths(-1).ToDateTimeUnspecified(),
					RetainedEarningsAccountNumber = _retainedEarnings.ToInt32(),
					ClosingGeneralLedgerEntryId = closingGeneralLedgerEntryIdentifier.ToGuid()
				})
				.Throws(new ClosingDateBeforePeriodException(AccountingPeriod.Open(openedOn), openedOn.PlusMonths(-1)))
				.Assert(_handler, _facts);
	}
}
