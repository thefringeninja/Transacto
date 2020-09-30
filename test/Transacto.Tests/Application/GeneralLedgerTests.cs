using System;
using System.Threading.Tasks;
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
		public Task opening_the_period(DateTimeOffset openedOn) =>
			new Scenario()
				.GivenNone()
				.When(new OpenGeneralLedger {
					OpenedOn = openedOn
				})
				.Then(GeneralLedger.Identifier, new GeneralLedgerOpened {
					OpenedOn = openedOn
				})
				.Assert(_handler, _facts);

		[Theory, AutoTransactoData]
		public Task closing_an_open_period(Period period,
			GeneralLedgerEntryIdentifier[] generalLedgerEntryIdentifiers,
			GeneralLedgerEntryIdentifier closingGeneralLedgerEntryIdentifier) =>
			new Scenario()
				.Given(GeneralLedger.Identifier, new GeneralLedgerOpened {
					OpenedOn = new DateTimeOffset(new DateTime(period.Year, period.Month, 2))
				})
				.When(new BeginClosingAccountingPeriod {
					GeneralLedgerEntryIds = Array.ConvertAll(generalLedgerEntryIdentifiers, x => x.ToGuid()),
					ClosingOn = new DateTimeOffset(new DateTime(period.Year, period.Month, 2)),
					RetainedEarningsAccountNumber = _retainedEarnings.ToInt32(),
					ClosingGeneralLedgerEntryId = closingGeneralLedgerEntryIdentifier.ToGuid()
				})
				.Then(GeneralLedger.Identifier, new AccountingPeriodClosing {
					Period = period.ToString(),
					GeneralLedgerEntryIds = Array.ConvertAll(generalLedgerEntryIdentifiers, x => x.ToGuid()),
					ClosingOn = new DateTimeOffset(new DateTime(period.Year, period.Month, 2)),
					RetainedEarningsAccountNumber = _retainedEarnings.ToInt32(),
					ClosingGeneralLedgerEntryId = closingGeneralLedgerEntryIdentifier.ToGuid()
				})
				.Assert(_handler, _facts);

		[Theory, AutoTransactoData]
		public Task closing_a_closed_period(Period period,
			GeneralLedgerEntryIdentifier closingGeneralLedgerEntryIdentifier) =>
			new Scenario()
				.Given(GeneralLedger.Identifier, new GeneralLedgerOpened {
						OpenedOn = new DateTimeOffset(new DateTime(period.Year, period.Month, 1))
					},
					new AccountingPeriodClosing {
						Period = period.ToString(),
						ClosingOn = new DateTimeOffset(new DateTime(period.Year, period.Month, 2)),
						RetainedEarningsAccountNumber = _retainedEarnings.ToInt32(),
						ClosingGeneralLedgerEntryId = closingGeneralLedgerEntryIdentifier.ToGuid()
					})
				.When(new BeginClosingAccountingPeriod {
					ClosingOn = new DateTimeOffset(new DateTime(period.Year, period.Month, 2)),
					RetainedEarningsAccountNumber = _retainedEarnings.ToInt32(),
					ClosingGeneralLedgerEntryId = closingGeneralLedgerEntryIdentifier.ToGuid()
				})
				.Throws(new PeriodClosingInProcessException(period))
				.Assert(_handler, _facts);

		[Theory, AutoTransactoData]
		public Task closing_the_period_before_the_period_has_started(DateTimeOffset openedOn,
			GeneralLedgerEntryIdentifier closingGeneralLedgerEntryIdentifier) =>
			new Scenario()
				.Given(GeneralLedger.Identifier, new GeneralLedgerOpened {
					OpenedOn = openedOn
				})
				.When(new BeginClosingAccountingPeriod {
					ClosingOn = openedOn.AddMonths(-1),
					RetainedEarningsAccountNumber = _retainedEarnings.ToInt32(),
					ClosingGeneralLedgerEntryId = closingGeneralLedgerEntryIdentifier.ToGuid()
				})
				.Throws(new ClosingDateBeforePeriodException(Period.Open(openedOn), openedOn.AddMonths(-1)))
				.Assert(_handler, _facts);
	}
}
