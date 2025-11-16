using System.Collections.Immutable;
using NodaTime;
using Transacto.Domain;
using Transacto.Messages;
using Transacto.Testing;

namespace Transacto;

public static class AccountClosingProcessSpecifications {
	public class UnpostedEntryThrows(
		LocalDate openedOn,
		GeneralLedgerEntryNumber generalLedgerEntryNumber,
		EquityAccount retainedEarnings,
		GeneralLedgerEntryIdentifier generalLedgerEntryIdentifier,
		GeneralLedgerEntryIdentifier closingGeneralLedgerEntryIdentifier) : ExceptionSpecification(new Scenario()
		.Given(GeneralLedger.Identifier,
			new GeneralLedgerOpened {
				OpenedOn = Time.Format.LocalDate(openedOn)
			},
			new AccountingPeriodClosing {
				Period = AccountingPeriod.Open(openedOn).ToString(),
				ClosingOn = Time.Format.LocalDateTime(openedOn.AtMidnight()),
				RetainedEarningsAccountNumber = retainedEarnings.AccountNumber.ToInt32(),
				ClosingGeneralLedgerEntryId = closingGeneralLedgerEntryIdentifier.ToGuid(),
				GeneralLedgerEntryIds = ImmutableArray<Guid>.Empty.Add(generalLedgerEntryIdentifier.ToGuid())
			})
		.Given(ChartOfAccounts.Identifier,
			new AccountDefined {
				AccountName = retainedEarnings.AccountName.ToString(),
				AccountNumber = retainedEarnings.AccountNumber.ToInt32()
			})
		.Given(GeneralLedgerEntry.FormatStreamIdentifier(generalLedgerEntryIdentifier),
			new GeneralLedgerEntryCreated {
				GeneralLedgerEntryId = generalLedgerEntryIdentifier.ToGuid(),
				ReferenceNumber = generalLedgerEntryNumber.ToString(),
				Period = AccountingPeriod.Open(openedOn).ToString(),
				CreatedOn = Time.Format.LocalDateTime(openedOn.AtMidnight())
			})
		.When(new AccountingPeriodClosing {
			Period = AccountingPeriod.Open(openedOn).ToString(),
			ClosingOn = Time.Format.LocalDateTime(openedOn.AtMidnight()),
			RetainedEarningsAccountNumber = retainedEarnings.AccountNumber.ToInt32(),
			ClosingGeneralLedgerEntryId = closingGeneralLedgerEntryIdentifier.ToGuid(),
			GeneralLedgerEntryIds = ImmutableArray<Guid>.Empty.Add(generalLedgerEntryIdentifier.ToGuid())
		})
		.Throws(new GeneralLedgerEntryWasNotPostedException(generalLedgerEntryIdentifier)));

	public class PeriodClosingStarted(
		LocalDate openedOn,
		EquityAccount retainedEarnings,
		AssetAccount cashAccount,
		IncomeAccount incomeAccount,
		ExpenseAccount expenseAccount,
		GeneralLedgerEntryIdentifier[] generalLedgerEntryIdentifiers,
		GeneralLedgerEntryIdentifier closingGeneralLedgerEntryIdentifier,
		Money income,
		Money expense)
		: EventSpecification(new Func<IEventCentricTestSpecificationBuilder>(() => {
			var accountingPeriodClosing = new AccountingPeriodClosing {
				Period = AccountingPeriod.Open(openedOn).ToString(),
				ClosingOn = Time.Format.LocalDateTime(openedOn.AtMidnight()),
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
								ReferenceNumber = $"sale-{index}",
								Period = AccountingPeriod.Open(openedOn).ToString(),
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
								Period = AccountingPeriod.Open(openedOn).ToString(),
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
						CreatedOn = Time.Format.LocalDateTime(openedOn.AtMidnight()),
						GeneralLedgerEntryId = closingGeneralLedgerEntryIdentifier.ToGuid(),
						ReferenceNumber = $"jec-{AccountingPeriod.Open(openedOn)}",
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
