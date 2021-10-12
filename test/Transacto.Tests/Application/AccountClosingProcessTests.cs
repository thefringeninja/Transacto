using System;
using System.Linq;
using System.Threading.Tasks;
using NodaTime;
using Transacto.Domain;
using Transacto.Messages;
using Transacto.Testing;
using Xunit;

namespace Transacto.Application {
	public class AccountClosingProcessTests {
		private readonly AccountingPeriodClosingHandlers _handler;
		private readonly IFactRecorder _facts;

		public AccountClosingProcessTests() {
			_facts = new FactRecorder();
			_handler = new AccountingPeriodClosingHandlers(
				new GeneralLedgerTestRepository(_facts),
				new GeneralLedgerEntryTestRepository(_facts),
				new ChartOfAccountsTestRepository(_facts),
				_ => false);
		}

		[Theory, AutoTransactoData]
		public Task unposted_entry_throws(LocalDate openedOn, GeneralLedgerEntryNumber generalLedgerEntryNumber,
			EquityAccount retainedEarnings, GeneralLedgerEntryIdentifier generalLedgerEntryIdentifier,
			GeneralLedgerEntryIdentifier closingGeneralLedgerEntryIdentifier) {
			var period = AccountingPeriod.Open(openedOn);
			var closingOn = openedOn.AtMidnight();

			var accountingPeriodClosing = new AccountingPeriodClosing {
				Period = period.ToString(),
				ClosingOn = Time.Format.LocalDateTime(closingOn),
				RetainedEarningsAccountNumber = retainedEarnings.AccountNumber.ToInt32(),
				ClosingGeneralLedgerEntryId = closingGeneralLedgerEntryIdentifier.ToGuid(),
				GeneralLedgerEntryIds = new[] {generalLedgerEntryIdentifier.ToGuid()}
			};
			return new Scenario()
				.Given(GeneralLedger.Identifier,
					new GeneralLedgerOpened {
						OpenedOn = Time.Format.LocalDate(openedOn)
					},
					accountingPeriodClosing)
				.Given(ChartOfAccounts.Identifier,
					new AccountDefined {
						AccountName = retainedEarnings.AccountName.ToString(),
						AccountNumber = retainedEarnings.AccountNumber.ToInt32()
					})
				.Given(GeneralLedgerEntry.FormatStreamIdentifier(generalLedgerEntryIdentifier),
					new GeneralLedgerEntryCreated {
						GeneralLedgerEntryId = generalLedgerEntryIdentifier.ToGuid(),
						ReferenceNumber = generalLedgerEntryNumber.ToString(),
						Period = period.ToString(),
						CreatedOn = Time.Format.LocalDateTime(openedOn.AtMidnight())
					})
				.When(accountingPeriodClosing)
				.Throws(new GeneralLedgerEntryWasNotPostedException(generalLedgerEntryIdentifier))
				.Assert(_handler, _facts);
		}

		[Theory, AutoTransactoData]
		public Task period_closing_started(LocalDate openedOn, EquityAccount retainedEarnings,
			AssetAccount cashAccount, IncomeAccount incomeAccount, ExpenseAccount expenseAccount,
			GeneralLedgerEntryIdentifier[] generalLedgerEntryIdentifiers,
			GeneralLedgerEntryIdentifier closingGeneralLedgerEntryIdentifier, Money income, Money expense) {
			var period = AccountingPeriod.Open(openedOn);

			var closingOn = openedOn.AtMidnight();

			var accountingPeriodClosing = new AccountingPeriodClosing {
				Period = period.ToString(),
				ClosingOn = Time.Format.LocalDateTime(closingOn),
				RetainedEarningsAccountNumber = retainedEarnings.AccountNumber.ToInt32(),
				ClosingGeneralLedgerEntryId = closingGeneralLedgerEntryIdentifier.ToGuid(),
				GeneralLedgerEntryIds =
					Array.ConvertAll(generalLedgerEntryIdentifiers, identifier => identifier.ToGuid())
			};
			var net = income - expense;
			var generalLedgerEntryFacts = generalLedgerEntryIdentifiers.SelectMany(
					(identifier, index) => Array.ConvertAll(new object[] {
						new GeneralLedgerEntryCreated {
							ReferenceNumber = $"sale-{index}",
							Period = period.ToString(),
							CreatedOn = Time.Format.LocalDate(openedOn),
							GeneralLedgerEntryId = identifier.ToGuid()
						},
						new CreditApplied {
							Amount = income.ToDecimal(),
							AccountNumber = incomeAccount.AccountNumber.ToInt32(),
							GeneralLedgerEntryId = identifier.ToGuid()
						},
						new DebitApplied {
							Amount = expense.ToDecimal(),
							AccountNumber = expenseAccount.AccountNumber.ToInt32(),
							GeneralLedgerEntryId = identifier.ToGuid()
						},
						net > Money.Zero
							? new DebitApplied {
								Amount = net.ToDecimal(),
								AccountNumber = cashAccount.AccountNumber.ToInt32(),
								GeneralLedgerEntryId = identifier.ToGuid()
							}
							: new CreditApplied {
								Amount = -net.ToDecimal(),
								AccountNumber = cashAccount.AccountNumber.ToInt32(),
								GeneralLedgerEntryId = identifier.ToGuid()
							},
						new GeneralLedgerEntryPosted {
							Period = period.ToString(),
							GeneralLedgerEntryId = identifier.ToGuid()
						},
					}, e => new Fact(GeneralLedgerEntry.FormatStreamIdentifier(identifier), e)))
				.ToArray();
			return new Scenario()
				.Given(ChartOfAccounts.Identifier,
					new AccountDefined {
						AccountName = cashAccount.AccountName.ToString(),
						AccountNumber = cashAccount.AccountNumber.ToInt32()
					},
					new AccountDefined {
						AccountName = incomeAccount.AccountName.ToString(),
						AccountNumber = incomeAccount.AccountNumber.ToInt32()
					},
					new AccountDefined {
						AccountName = expenseAccount.AccountName.ToString(),
						AccountNumber = expenseAccount.AccountNumber.ToInt32()
					},
					new AccountDefined {
						AccountName = retainedEarnings.AccountName.ToString(),
						AccountNumber = retainedEarnings.AccountNumber.ToInt32()
					})
				.Given(GeneralLedger.Identifier,
					new GeneralLedgerOpened {
						OpenedOn = Time.Format.LocalDate(openedOn)
					},
					accountingPeriodClosing)
				.Given(generalLedgerEntryFacts)
				.When(accountingPeriodClosing)
				.Then(GeneralLedger.Identifier,
					new GeneralLedgerEntryCreated {
						CreatedOn = Time.Format.LocalDateTime(closingOn),
						GeneralLedgerEntryId = closingGeneralLedgerEntryIdentifier.ToGuid(),
						ReferenceNumber = $"jec-{period}",
						Period = period.ToString()
					},
					new DebitApplied {
						Amount = income.ToDecimal() * generalLedgerEntryIdentifiers.Length,
						AccountNumber = incomeAccount.AccountNumber.ToInt32(),
						GeneralLedgerEntryId = closingGeneralLedgerEntryIdentifier.ToGuid()
					},
					new CreditApplied {
						Amount = expense.ToDecimal() * generalLedgerEntryIdentifiers.Length,
						AccountNumber = expenseAccount.AccountNumber.ToInt32(),
						GeneralLedgerEntryId = closingGeneralLedgerEntryIdentifier.ToGuid()
					},
					net < Money.Zero
						? new DebitApplied {
							Amount = -net.ToDecimal() * generalLedgerEntryIdentifiers.Length,
							AccountNumber = retainedEarnings.AccountNumber.ToInt32(),
							GeneralLedgerEntryId = closingGeneralLedgerEntryIdentifier.ToGuid()
						}
						: new CreditApplied {
							Amount = net.ToDecimal() * generalLedgerEntryIdentifiers.Length,
							AccountNumber = retainedEarnings.AccountNumber.ToInt32(),
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
								AccountNumber = cashAccount.AccountNumber.ToInt32(),
								Amount = net.ToDecimal() * generalLedgerEntryIdentifiers.Length
							},
							new BalanceLineItem {
								AccountNumber = retainedEarnings.AccountNumber.ToInt32(),
								Amount = net.ToDecimal() * generalLedgerEntryIdentifiers.Length
							},
							new BalanceLineItem {
								AccountNumber = incomeAccount.AccountNumber.ToInt32(),
								Amount = Money.Zero.ToDecimal()
							},
							new BalanceLineItem {
								AccountNumber = expenseAccount.AccountNumber.ToInt32(),
								Amount = Money.Zero.ToDecimal()
							}
						}
					})
				.Assert(_handler, _facts);
		}
	}
}
