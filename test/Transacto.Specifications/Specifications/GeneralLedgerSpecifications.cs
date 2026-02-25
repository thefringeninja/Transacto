using System.Collections.Immutable;
using NodaTime;
using Transacto.Domain;
using Transacto.Messages;
using Transacto.Testing;

namespace Transacto.Specifications;

public static class GeneralLedgerSpecifications {
	public class OpeningThePeriod(LocalDate openedOn) : EventSpecification(new Scenario()
		.GivenNone()
		.When(new OpenGeneralLedger {
			OpenedOn = openedOn
		})
		.Then(GeneralLedger.Identifier, new GeneralLedgerOpened {
			OpenedOn = openedOn
		}));

	public class ClosingAnOpenPeriod(
		YearMonth openedOn,
		EquityAccount retainedEarnings,
		AssetAccount accountsReceivable,
		LiabilityAccount accountsPayable,
		GeneralLedgerEntryIdentifier[] generalLedgerEntryIdentifiers,
		GeneralLedgerEntryIdentifier closingGeneralLedgerEntryIdentifier) : EventSpecification(new Scenario()
		.Given(ChartOfAccounts.Identifier, new ChartOfAccountsConfigured {
			AccountNumberWidth = AccountNumberWidth.Min.ToInt32(),
			RetainedEarnings = retainedEarnings.AccountNumber.ToInt32(),
			AccountsPayable = accountsPayable.AccountNumber.ToInt32(),
			AccountsReceivable = accountsReceivable.AccountNumber.ToInt32()
		}, new AccountDefined {
			AccountNumber = retainedEarnings.AccountNumber.ToInt32(),
			AccountName = retainedEarnings.AccountName.ToString(),
			CashFlowCategory = retainedEarnings.CashFlowCategory.ToEnum(),
			AffectsCash = retainedEarnings.AffectsCash
		})
		.Given(GeneralLedger.Identifier, new GeneralLedgerOpened {
			OpenedOn = openedOn.OnDayOfMonth(1)
		})
		.When(new BeginClosingAccountingPeriod {
			GeneralLedgerEntryIds =
				ImmutableArray<Guid>.Empty.AddRange(Array.ConvertAll(generalLedgerEntryIdentifiers,
					x => x.ToGuid())),
			ClosingOn = openedOn.OnDayOfMonth(2),
			RetainedEarningsAccountNumber = retainedEarnings.AccountNumber.ToInt32(),
			ClosingGeneralLedgerEntryId = closingGeneralLedgerEntryIdentifier.ToGuid()
		})
		.Then(GeneralLedger.Identifier, new AccountingPeriodClosing {
			Period = AccountingPeriod.Open(openedOn.OnDayOfMonth(1)).ToString(),
			GeneralLedgerEntryIds =
				[..Array.ConvertAll(generalLedgerEntryIdentifiers, x => x.ToGuid())],
			ClosingOn = openedOn.OnDayOfMonth(2),
			RetainedEarningsAccountNumber = retainedEarnings.AccountNumber.ToInt32(),
			ClosingGeneralLedgerEntryId = closingGeneralLedgerEntryIdentifier.ToGuid()
		}));

	public class ClosingAClosedPeriod(
		YearMonth openedOn,
		EquityAccount retainedEarnings,
		AssetAccount accountsReceivable,
		LiabilityAccount accountsPayable,
		GeneralLedgerEntryIdentifier closingGeneralLedgerEntryIdentifier) : ExceptionSpecification(new Scenario()
		.Given(ChartOfAccounts.Identifier, new ChartOfAccountsConfigured {
			AccountNumberWidth = AccountNumberWidth.Min.ToInt32(),
			RetainedEarnings = retainedEarnings.AccountNumber.ToInt32(),
			AccountsPayable = accountsPayable.AccountNumber.ToInt32(),
			AccountsReceivable = accountsReceivable.AccountNumber.ToInt32()
		}, new AccountDefined {
			AccountNumber = retainedEarnings.AccountNumber.ToInt32(),
			AccountName = retainedEarnings.AccountName.ToString(),
			CashFlowCategory = retainedEarnings.CashFlowCategory.ToEnum(),
			AffectsCash = retainedEarnings.AffectsCash
		})
		.Given(GeneralLedger.Identifier, new GeneralLedgerOpened {
				OpenedOn = openedOn.OnDayOfMonth(1)
			},
			new AccountingPeriodClosing {
				Period = AccountingPeriod.Open(openedOn.OnDayOfMonth(1)).ToString(),
				ClosingOn = openedOn.OnDayOfMonth(2),
				RetainedEarningsAccountNumber = retainedEarnings.AccountNumber.ToInt32(),
				ClosingGeneralLedgerEntryId = closingGeneralLedgerEntryIdentifier.ToGuid(),
				GeneralLedgerEntryIds = ImmutableArray<Guid>.Empty
			})
		.When(new BeginClosingAccountingPeriod {
			ClosingOn = openedOn.OnDayOfMonth(2),
			RetainedEarningsAccountNumber = retainedEarnings.AccountNumber.ToInt32(),
			ClosingGeneralLedgerEntryId = closingGeneralLedgerEntryIdentifier.ToGuid(),
			GeneralLedgerEntryIds = ImmutableArray<Guid>.Empty
		})
		.Throws(new PeriodClosingInProcessException(AccountingPeriod.Open(openedOn.OnDayOfMonth(1)))));

	public class ClosingThePeriodBeforeThePeriodHasStarted(
		LocalDate openedOn,
		EquityAccount retainedEarnings,
		AssetAccount accountsReceivable,
		LiabilityAccount accountsPayable,
		GeneralLedgerEntryIdentifier closingGeneralLedgerEntryIdentifier) : ExceptionSpecification(new Scenario()
		.Given(ChartOfAccounts.Identifier, new ChartOfAccountsConfigured {
			AccountNumberWidth = AccountNumberWidth.Min.ToInt32(),
			RetainedEarnings = retainedEarnings.AccountNumber.ToInt32(),
			AccountsPayable = accountsPayable.AccountNumber.ToInt32(),
			AccountsReceivable = accountsReceivable.AccountNumber.ToInt32()

		}, new AccountDefined {
			AccountNumber = retainedEarnings.AccountNumber.ToInt32(),
			AccountName = retainedEarnings.AccountName.ToString(),
			CashFlowCategory = retainedEarnings.CashFlowCategory.ToEnum(),
			AffectsCash = retainedEarnings.AffectsCash
		})
		.Given(GeneralLedger.Identifier, new GeneralLedgerOpened {
			OpenedOn = openedOn
		})
		.When(new BeginClosingAccountingPeriod {
			ClosingOn = openedOn.PlusMonths(-1),
			RetainedEarningsAccountNumber = retainedEarnings.AccountNumber.ToInt32(),
			ClosingGeneralLedgerEntryId = closingGeneralLedgerEntryIdentifier.ToGuid(),
			GeneralLedgerEntryIds = ImmutableArray<Guid>.Empty
		})
		.Throws(new ClosingDateBeforePeriodException(AccountingPeriod.Open(openedOn), openedOn.PlusMonths(-1))));
}
