using System.Collections.Immutable;
using NodaTime;
using Transacto.Domain;
using Transacto.Messages;
using Transacto.Testing;

namespace Transacto.Specifications;

public static class AccountClosingProcessSpecifications {
	public class UnpostedEntryThrows(
		LocalDate openedOn,
		EquityAccount retainedEarnings,
		GeneralLedgerEntryIdentifier generalLedgerEntryIdentifier,
		GeneralLedgerEntryIdentifier closingGeneralLedgerEntryIdentifier) : ExceptionSpecification(new Scenario()
		.Given(ChartOfAccounts.Identifier,
			new ChartOfAccountsConfigured {
				AccountNumberWidth = AccountNumberWidth.Min.ToInt32(),
				AccountsPayable = 2000,
				AccountsReceivable = 1200,
				RetainedEarnings = retainedEarnings.AccountNumber.ToInt32(),
			},
			new AccountDefined {
				AccountNumber = retainedEarnings.AccountNumber.ToInt32(),
				AccountName = retainedEarnings.AccountName.ToString(),
				AffectsCash = retainedEarnings.AffectsCash,
				CashFlowCategory = retainedEarnings.CashFlowCategory.ToEnum()
			})
		.Given(GeneralLedger.Identifier,
			new GeneralLedgerOpened {
				OpenedOn = openedOn
			},
			new AccountingPeriodClosing {
				Period = AccountingPeriod.Open(openedOn).ToString(),
				ClosingOn = openedOn,
				RetainedEarningsAccountNumber = retainedEarnings.AccountNumber.ToInt32(),
				ClosingGeneralLedgerEntryId = closingGeneralLedgerEntryIdentifier.ToGuid(),
				GeneralLedgerEntryIds = ImmutableArray<Guid>.Empty.Add(generalLedgerEntryIdentifier.ToGuid())
			})
		.Given(ChartOfAccounts.Identifier,
			new AccountDefined {
				AccountName = retainedEarnings.AccountName.ToString(),
				AccountNumber = retainedEarnings.AccountNumber.ToInt32(),
				CashFlowCategory = retainedEarnings.CashFlowCategory.ToEnum(),
				AffectsCash = retainedEarnings.AffectsCash
			})
		.Given(GeneralLedgerEntry.FormatStreamIdentifier(generalLedgerEntryIdentifier),
			new GeneralLedgerEntryCreated {
				GeneralLedgerEntryId = generalLedgerEntryIdentifier.ToGuid(),
				Period = AccountingPeriod.Open(openedOn).ToString(),
				CreatedOn = openedOn
			})
		.When(new AccountingPeriodClosing {
			Period = AccountingPeriod.Open(openedOn).ToString(),
			ClosingOn = openedOn,
			RetainedEarningsAccountNumber = retainedEarnings.AccountNumber.ToInt32(),
			ClosingGeneralLedgerEntryId = closingGeneralLedgerEntryIdentifier.ToGuid(),
			GeneralLedgerEntryIds = ImmutableArray<Guid>.Empty.Add(generalLedgerEntryIdentifier.ToGuid())
		})
		.Throws(new GeneralLedgerEntryWasNotPostedException(generalLedgerEntryIdentifier)));

	public class PeriodClosingStarted(
		LocalDate openedOn,
		EquityAccount retainedEarnings,
		AssetAccount cashAccount,
		AssetAccount accountsReceivable,
		LiabilityAccount accountsPayable,
		IncomeAccount incomeAccount,
		ExpenseAccount expenseAccount,
		GeneralLedgerEntryIdentifier[] generalLedgerEntryIdentifiers,
		GeneralLedgerEntryIdentifier closingGeneralLedgerEntryIdentifier,
		Money income,
		Money expense)
		: EventSpecification(new Func<IEventCentricTestSpecificationBuilder>(() => {
			var accountingPeriodClosing = new AccountingPeriodClosing {
				Period = AccountingPeriod.Open(openedOn).ToString(),
				ClosingOn = openedOn,
				RetainedEarningsAccountNumber = retainedEarnings.AccountNumber.ToInt32(),
				ClosingGeneralLedgerEntryId = closingGeneralLedgerEntryIdentifier.ToGuid(),
				GeneralLedgerEntryIds =
					[..Array.ConvertAll(generalLedgerEntryIdentifiers, identifier => identifier.ToGuid())]
			};
			var net = income - expense;
			var generalLedgerEntryFacts = generalLedgerEntryIdentifiers.SelectMany((identifier, index) =>
					Array.ConvertAll(
						new object[] {
							new GeneralLedgerEntryCreated {
								Period = AccountingPeriod.Open(openedOn).ToString(),
								CreatedOn = openedOn,
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
								Period = AccountingPeriod.Open(openedOn).ToString(),
								GeneralLedgerEntryId = identifier.ToGuid()
							},
						}, e => new Fact(GeneralLedgerEntry.FormatStreamIdentifier(identifier), e)))
				.ToArray();
			return new Scenario()
				.Given(ChartOfAccounts.Identifier,
					new ChartOfAccountsConfigured {
						AccountNumberWidth = AccountNumberWidth.Min.ToInt32(),
						AccountsReceivable = accountsReceivable.AccountNumber.ToInt32(),
						AccountsPayable = accountsPayable.AccountNumber.ToInt32(),
						RetainedEarnings = retainedEarnings.AccountNumber.ToInt32()
					},
					new AccountDefined {
						AccountName = cashAccount.AccountName.ToString(),
						AccountNumber = cashAccount.AccountNumber.ToInt32(),
						CashFlowCategory = cashAccount.CashFlowCategory.ToEnum(),
						AffectsCash = cashAccount.AffectsCash
					},
					new AccountDefined {
						AccountName = accountsReceivable.AccountName.ToString(),
						AccountNumber = accountsReceivable.AccountNumber.ToInt32(),
						CashFlowCategory = accountsReceivable.CashFlowCategory.ToEnum(),
						AffectsCash = accountsReceivable.AffectsCash
					},
					new AccountDefined {
						AccountName = accountsPayable.AccountName.ToString(),
						AccountNumber = accountsPayable.AccountNumber.ToInt32(),
						CashFlowCategory = accountsPayable.CashFlowCategory.ToEnum(),
						AffectsCash = accountsPayable.AffectsCash
					},
					new AccountDefined {
						AccountName = incomeAccount.AccountName.ToString(),
						AccountNumber = incomeAccount.AccountNumber.ToInt32(),
						CashFlowCategory = incomeAccount.CashFlowCategory.ToEnum(),
						AffectsCash = incomeAccount.AffectsCash
					},
					new AccountDefined {
						AccountName = expenseAccount.AccountName.ToString(),
						AccountNumber = expenseAccount.AccountNumber.ToInt32(),
						CashFlowCategory = expenseAccount.CashFlowCategory.ToEnum(),
						AffectsCash = expenseAccount.AffectsCash
					},
					new AccountDefined {
						AccountName = retainedEarnings.AccountName.ToString(),
						AccountNumber = retainedEarnings.AccountNumber.ToInt32(),
						CashFlowCategory = retainedEarnings.CashFlowCategory.ToEnum(),
						AffectsCash = retainedEarnings.AffectsCash
					})
				.Given(GeneralLedger.Identifier,
					new GeneralLedgerOpened {
						OpenedOn = openedOn
					},
					accountingPeriodClosing)
				.Given(generalLedgerEntryFacts)
				.When(accountingPeriodClosing)
				.Then(GeneralLedger.Identifier,
					new GeneralLedgerEntryCreated {
						CreatedOn = openedOn,
						GeneralLedgerEntryId = closingGeneralLedgerEntryIdentifier.ToGuid(),
						Period = AccountingPeriod.Open(openedOn).ToString()
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
						Period = AccountingPeriod.Open(openedOn).ToString(),
						GeneralLedgerEntryId = closingGeneralLedgerEntryIdentifier.ToGuid()
					},
					new AccountingPeriodClosed {
						Period = AccountingPeriod.Open(openedOn).ToString(),
						GeneralLedgerEntryIds =
							[..Array.ConvertAll(generalLedgerEntryIdentifiers, identifier => identifier.ToGuid())],
						ClosingGeneralLedgerEntryId = closingGeneralLedgerEntryIdentifier.ToGuid(),
						Balance = [
							..new[] {
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
						]
					});
		})()) {
		private static IEventCentricTestSpecificationBuilder CreateSpecification() {
			throw new NotImplementedException();
		}
	}
}
