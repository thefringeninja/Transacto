using System;
using System.Collections.Generic;
using System.Linq;
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
				new GeneralLedgerTestRepository(_facts),
				new GeneralLedgerEntryTestRepository(_facts),
				_ => false);
			_retainedEarnings = new AccountNumber(new Random().Next(3000, 3999));
		}

		[Theory, AutoTransactoData]
		public Task opening_the_period(DateTimeOffset openedOn) =>
			new Scenario()
				.GivenNone()
				.When(new OpenGeneralLedger {
					OpenedOn = openedOn
				})
				.Then("generalLedger", new GeneralLedgerOpened {
					OpenedOn = openedOn
				})
				.Assert(_handler, _facts);

		[Theory, AutoTransactoData]
		public Task closing_an_open_period(Period period,
			GeneralLedgerEntryIdentifier[] generalLedgerEntryIdentifiers,
			GeneralLedgerEntryIdentifier closingGeneralLedgerEntryIdentifier) =>
			new Scenario()
				.Given("generalLedger", new GeneralLedgerOpened {
					OpenedOn = new DateTimeOffset(new DateTime(period.Year, period.Month, 2))
				})
				.When(new BeginClosingAccountingPeriod {
					GeneralLedgerEntryIds = Array.ConvertAll(generalLedgerEntryIdentifiers, x => x.ToGuid()),
					ClosingOn = new DateTimeOffset(new DateTime(period.Year, period.Month, 2)),
					RetainedEarningsAccountNumber = _retainedEarnings.ToInt32(),
					ClosingGeneralLedgerEntryId = closingGeneralLedgerEntryIdentifier.ToGuid()
				})
				.Then("generalLedger", new AccountingPeriodClosing {
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
				.Given("generalLedger", new GeneralLedgerOpened {
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
				.Given("generalLedger", new GeneralLedgerOpened {
					OpenedOn = openedOn
				})
				.When(new BeginClosingAccountingPeriod {
					ClosingOn = openedOn.AddMonths(-1),
					RetainedEarningsAccountNumber = _retainedEarnings.ToInt32(),
					ClosingGeneralLedgerEntryId = closingGeneralLedgerEntryIdentifier.ToGuid()
				})
				.Throws(new ClosingDateBeforePeriodException(Period.Open(openedOn), openedOn.AddMonths(-1)))
				.Assert(_handler, _facts);

		[Theory, AutoTransactoData]
		public Task period_closing_started(DateTimeOffset openedOn,
			GeneralLedgerEntryIdentifier[] generalLedgerEntryIdentifiers,
			GeneralLedgerEntryIdentifier closingGeneralLedgerEntryIdentifier,
			Money amount) {
			var period = Period.Open(openedOn);
			var cashAccountNumber = new AccountNumber(new Random().Next(1000, 1999));
			var incomeAccountNumber = new AccountNumber(new Random().Next(4000, 4999));

			var closingOn = new DateTimeOffset(new DateTime(period.Year, period.Month, 2));

			var accountingPeriodClosing = new AccountingPeriodClosing {
				Period = period.ToString(),
				ClosingOn = closingOn,
				RetainedEarningsAccountNumber = _retainedEarnings.ToInt32(),
				ClosingGeneralLedgerEntryId = closingGeneralLedgerEntryIdentifier.ToGuid(),
				GeneralLedgerEntryIds =
					Array.ConvertAll(generalLedgerEntryIdentifiers, identifier => identifier.ToGuid())
			};
			var generalLedgerEntryFacts = generalLedgerEntryIdentifiers.SelectMany(
					(identifier, index) => Array.ConvertAll(new object[] {
						new GeneralLedgerEntryCreated {
							Number = $"sale-{index}",
							Period = period.ToString(),
							CreatedOn = openedOn,
							GeneralLedgerEntryId = identifier.ToGuid()
						},
						new DebitApplied {
							Amount = amount.ToDecimal(),
							AccountNumber = cashAccountNumber.ToInt32(),
							GeneralLedgerEntryId = identifier.ToGuid()
						},
						new CreditApplied {
							Amount = amount.ToDecimal(),
							AccountNumber = incomeAccountNumber.ToInt32(),
							GeneralLedgerEntryId = identifier.ToGuid()
						},
						new GeneralLedgerEntryPosted {
							Period = period.ToString(),
							GeneralLedgerEntryId = identifier.ToGuid()
						},
					}, e => new Fact($"generalLedgerEntry-{identifier}", e)))
				.ToArray();
			return new Scenario()
				.Given("chartOfAccounts",
					new AccountDefined {
						AccountName = "Cash on Hand",
						AccountNumber = cashAccountNumber.ToInt32()
					},
					new AccountDefined {
						AccountName = "Income",
						AccountNumber = incomeAccountNumber.ToInt32()
					},
					new AccountDefined {
						AccountName = "Retained Earnings",
						AccountNumber = _retainedEarnings.ToInt32()
					})
				.Given("generalLedger",
					new GeneralLedgerOpened {
						OpenedOn = openedOn
					},
					accountingPeriodClosing)
				.Given(generalLedgerEntryFacts)
				.When(accountingPeriodClosing)
				.Then("generalLedger",
					new GeneralLedgerEntryCreated {
						CreatedOn = closingOn,
						GeneralLedgerEntryId = closingGeneralLedgerEntryIdentifier.ToGuid(),
						Number = $"jec-{period}",
						Period = period.ToString()
					},
					new DebitApplied {
						Amount = amount.ToDecimal() * generalLedgerEntryIdentifiers.Length,
						AccountNumber = incomeAccountNumber.ToInt32(),
						GeneralLedgerEntryId = closingGeneralLedgerEntryIdentifier.ToGuid()
					},
					new CreditApplied {
						Amount = amount.ToDecimal() * generalLedgerEntryIdentifiers.Length,
						AccountNumber = _retainedEarnings.ToInt32(),
						GeneralLedgerEntryId = closingGeneralLedgerEntryIdentifier.ToGuid()
					},
					new GeneralLedgerEntryPosted {
						Period = period.ToString(),
						GeneralLedgerEntryId = closingGeneralLedgerEntryIdentifier.ToGuid()
					},
					new AccountingPeriodClosed {
						Period = period.ToString(),
						GeneralLedgerEntryIds = Array.ConvertAll(generalLedgerEntryIdentifiers,
							identifier => identifier.ToGuid()),
						ClosingGeneralLedgerEntryId = closingGeneralLedgerEntryIdentifier.ToGuid(),
						Balance = new[] {
							new BalanceLineItem {
								AccountNumber = cashAccountNumber.ToInt32(),
								Amount = amount.ToDecimal() * generalLedgerEntryIdentifiers.Length
							},
							new BalanceLineItem {
								AccountNumber = incomeAccountNumber.ToInt32(),
								Amount = Money.Zero.ToDecimal()
							},
							new BalanceLineItem {
								AccountNumber = _retainedEarnings.ToInt32(),
								Amount = -(amount.ToDecimal() * generalLedgerEntryIdentifiers.Length)
							}
						}
					})
				.Assert(_handler, _facts);
		}
	}
}
