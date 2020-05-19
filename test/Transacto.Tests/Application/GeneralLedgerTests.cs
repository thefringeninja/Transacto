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

		public GeneralLedgerTests() {
			_facts = new FactRecorder();
			_handler = new GeneralLedgerHandlers(new GeneralLedgerTestRepository(_facts));
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
			GeneralLedgerEntryIdentifier[] generalLedgerEntryIdentifiers) =>
			new Scenario()
				.Given(GeneralLedger.Identifier, new GeneralLedgerOpened {
					OpenedOn = new DateTimeOffset(new DateTime(period.Year, period.Month, 2))
				})
				.When(new BeginClosingAccountingPeriod {
					Period = period.ToString(),
					GeneralLedgerEntryIds = Array.ConvertAll(generalLedgerEntryIdentifiers, x => x.ToGuid()),
					ClosingOn = new DateTimeOffset(new DateTime(period.Year, period.Month, 2))
				})
				.Then(GeneralLedger.Identifier, new AccountingPeriodClosing {
					Period = period.ToString(),
					GeneralLedgerEntryIds = Array.ConvertAll(generalLedgerEntryIdentifiers, x => x.ToGuid()),
					ClosingOn = new DateTimeOffset(new DateTime(period.Year, period.Month, 2))
				})
				.Assert(_handler, _facts);

		[Theory, AutoTransactoData]
		public Task closing_a_closed_period(Period period) =>
			new Scenario()
				.Given(GeneralLedger.Identifier, new GeneralLedgerOpened {
					OpenedOn = new DateTimeOffset(new DateTime(period.Year, period.Month, 1))
				}, new BeginClosingAccountingPeriod {
					Period = period.ToString()
				})
				.When(new BeginClosingAccountingPeriod {
					Period = period.ToString()
				})
				.ThenNone()
				.Assert(_handler, _facts);
	}
}
