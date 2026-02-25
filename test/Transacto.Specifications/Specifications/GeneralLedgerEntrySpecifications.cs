using System.Text;
using NodaTime;
using Transacto.Domain;
using Transacto.Messages;
using Transacto.Testing;

namespace Transacto.Specifications;

public static class GeneralLedgerEntrySpecifications {
	public class PostingAnEntry(
		GeneralLedgerEntryIdentifier generalLedgerEntryIdentifier,
#pragma warning disable CS9113 // Parameter is unread.
		GeneralLedgerEntrySequenceNumber sequenceNumber,
#pragma warning restore CS9113 // Parameter is unread.
		LocalDate openedOn,
		AssetAccount assetAccount,
		LiabilityAccount liabilityAccount,
		Money amount) : EventSpecification(new Scenario()
		.Given(ChartOfAccounts.Identifier,
			new ChartOfAccountsConfigured {
				AccountNumberWidth = AccountNumberWidth.Min.ToInt32(),
				AccountsPayable = 2000,
				AccountsReceivable = 1200,
				RetainedEarnings = 3900,
			},
			new AccountDefined {
				AccountNumber = assetAccount.AccountNumber.ToInt32(),
				AccountName = assetAccount.AccountName.ToString(),
				AffectsCash = assetAccount.AffectsCash,
				CashFlowCategory = assetAccount.CashFlowCategory.ToEnum()
			},
			new AccountDefined {
				AccountNumber = liabilityAccount.AccountNumber.ToInt32(),
				AccountName = liabilityAccount.AccountName.ToString(),
				AffectsCash = liabilityAccount.AffectsCash,
				CashFlowCategory = liabilityAccount.CashFlowCategory.ToEnum()
			})
		.Given(GeneralLedger.Identifier,
			new GeneralLedgerOpened {
				OpenedOn = openedOn
			})
		.When(new PostGeneralLedgerEntry {
			GeneralLedgerEntryId = generalLedgerEntryIdentifier.ToGuid(),
			Period = AccountingPeriod.Open(openedOn).ToString(),
			BusinessTransaction = new TestTransaction(
				[new Debit(assetAccount.AccountNumber, amount)],
				[new Credit(liabilityAccount.AccountNumber, amount)]),
			CreatedOn = openedOn
		})
		.Then(GeneralLedgerEntry.FormatStreamIdentifier(generalLedgerEntryIdentifier),
			new GeneralLedgerEntryCreated {
				Period = AccountingPeriod.Open(openedOn).ToString(),
				CreatedOn = openedOn,
				GeneralLedgerEntryId = generalLedgerEntryIdentifier.ToGuid()
			},
			new CreditApplied {
				Amount = amount.ToDecimal(),
				AccountNumber = liabilityAccount.AccountNumber.ToInt32(),
				GeneralLedgerEntryId = generalLedgerEntryIdentifier.ToGuid()
			},
			new DebitApplied {
				Amount = amount.ToDecimal(),
				AccountNumber = assetAccount.AccountNumber.ToInt32(),
				GeneralLedgerEntryId = generalLedgerEntryIdentifier.ToGuid()
			},
			new TestTransaction(
				[new Debit(assetAccount.AccountNumber, amount)],
				[new Credit(liabilityAccount.AccountNumber, amount)]),
			new GeneralLedgerEntryPosted {
				Period = AccountingPeriod.Open(openedOn).ToString(),
				GeneralLedgerEntryId = generalLedgerEntryIdentifier.ToGuid()
			}));

	public class EntryDateNotInCurrentOrNextPeriodThrows(
		GeneralLedgerEntryIdentifier generalLedgerEntryIdentifier,
#pragma warning disable CS9113 // Parameter is unread.
		GeneralLedgerEntrySequenceNumber sequenceNumber,
#pragma warning restore CS9113 // Parameter is unread.
		LocalDate openedOn) : ExceptionSpecification(new Scenario()
		.Given(ChartOfAccounts.Identifier,
			new ChartOfAccountsConfigured {
				AccountNumberWidth = AccountNumberWidth.Min.ToInt32(),
				AccountsPayable = 2000,
				AccountsReceivable = 1200,
				RetainedEarnings = 3900,
			})
		.Given(GeneralLedger.Identifier,
			new GeneralLedgerOpened {
				OpenedOn = openedOn
			})
		.When(new PostGeneralLedgerEntry {
			GeneralLedgerEntryId = generalLedgerEntryIdentifier.ToGuid(),
			Period = AccountingPeriod.Open(openedOn).Next().Next().ToString(),
			BusinessTransaction = new TestTransaction(),
			CreatedOn = openedOn.PlusMonths(2)
		})
		.Throws(new GeneralLedgerEntryNotInPeriodException(generalLedgerEntryIdentifier,
			openedOn.PlusMonths(2),
			AccountingPeriod.Open(openedOn).Next()))
	);

	public class ApplyingDebitToNonExistingAccountThrows(
		GeneralLedgerEntryIdentifier generalLedgerEntryIdentifier,
#pragma warning disable CS9113 // Parameter is unread.
		GeneralLedgerEntryNumber referenceNumber,
#pragma warning restore CS9113 // Parameter is unread.
		LocalDate openedOn,
		AccountNumber accountNumber) : ExceptionSpecification(new Scenario()
		.Given(ChartOfAccounts.Identifier,
			new ChartOfAccountsConfigured {
				AccountNumberWidth = AccountNumberWidth.Min.ToInt32(),
				AccountsPayable = 2000,
				AccountsReceivable = 1200,
				RetainedEarnings = 3900,
			})
		.Given(GeneralLedger.Identifier,
			new GeneralLedgerOpened {
				OpenedOn = openedOn
			})
		.When(new PostGeneralLedgerEntry {
			GeneralLedgerEntryId = generalLedgerEntryIdentifier.ToGuid(),
			Period = AccountingPeriod.Open(openedOn).ToString(),
			BusinessTransaction = new TestTransaction([new Debit(accountNumber, Money.Zero)]),
			CreatedOn = openedOn
		})
		.Throws(new AccountDeactivatedException(accountNumber)));

	public class ApplyingCreditToNonExistingAccountThrows(
		GeneralLedgerEntryIdentifier generalLedgerEntryIdentifier,
#pragma warning disable CS9113 // Parameter is unread.
		GeneralLedgerEntrySequenceNumber sequenceNumber,
#pragma warning restore CS9113 // Parameter is unread.
		LocalDate openedOn,
		AccountNumber accountNumber) : ExceptionSpecification(new Scenario()
		.Given(ChartOfAccounts.Identifier,
			new ChartOfAccountsConfigured {
				AccountNumberWidth = AccountNumberWidth.Min.ToInt32(),
				AccountsPayable = 2000,
				AccountsReceivable = 1200,
				RetainedEarnings = 3900,
			})
		.Given(GeneralLedger.Identifier,
			new GeneralLedgerOpened {
				OpenedOn = openedOn
			})
		.When(new PostGeneralLedgerEntry {
			GeneralLedgerEntryId = generalLedgerEntryIdentifier.ToGuid(),
			Period = AccountingPeriod.Open(openedOn).ToString(),
			BusinessTransaction = new TestTransaction(credits: [new Credit(accountNumber, Money.Zero)]),
			CreatedOn = openedOn
		})
		.Throws(new AccountDeactivatedException(accountNumber)));

	public class EntryNotInBalanceThrows(
		GeneralLedgerEntryIdentifier generalLedgerEntryIdentifier,
#pragma warning disable CS9113 // Parameter is unread.
		GeneralLedgerEntrySequenceNumber sequenceNumber,
#pragma warning restore CS9113 // Parameter is unread.
		LocalDate openedOn,
		Account account) : ExceptionSpecification(new Scenario()
		.Given(GeneralLedger.Identifier,
			new GeneralLedgerOpened {
				OpenedOn = openedOn
			})
		.Given(ChartOfAccounts.Identifier,
			new AccountDefined {
				AccountName = account.AccountName.ToString(),
				AccountNumber = account.AccountNumber.ToInt32(),
				CashFlowCategory = account.CashFlowCategory.ToEnum(),
				AffectsCash = account.AffectsCash
			})
		.When(new PostGeneralLedgerEntry {
			Period = AccountingPeriod.Open(openedOn).ToString(),
			BusinessTransaction = new BadTransaction {
				Account = account.AccountNumber
			},
			CreatedOn = openedOn,
			GeneralLedgerEntryId = generalLedgerEntryIdentifier.ToGuid()
		})
		.Throws(new GeneralLedgerEntryNotInBalanceException(generalLedgerEntryIdentifier)));

	public class TestTransaction(
		Debit[]? debits = null,
		Credit[]? credits = null)
		: IBusinessTransaction {
		private readonly Debit[] _debits = debits ?? [];
		private readonly Credit[] _credits = credits ?? [];
		public GeneralLedgerEntrySequenceNumber? SequenceNumber { get; set; }
		public static GeneralLedgerEntryNumberPrefix Prefix { get; } = new("receiptOfGoods");

		public GeneralLedgerEntryNumber? ReferenceNumber =>
			SequenceNumber.HasValue ? new GeneralLedgerEntryNumber(Prefix, SequenceNumber.Value) : null;

		public IEnumerable<object> GetTransactionItems(ChartOfAccounts chartOfAccounts) =>
			_credits.Cast<object>().Concat(_debits.Cast<object>());

		public override string ToString() => GetTransactionItems(ChartOfAccounts.Factory())
			.Aggregate(new StringBuilder("Test transaction ").Append(SequenceNumber).AppendLine(),
				(acc, e) => acc.Append('\t').Append(e).AppendLine())
			.ToString()
			.TrimEnd();
	}

	public class BadTransaction : IBusinessTransaction {
		public GeneralLedgerEntrySequenceNumber? SequenceNumber { get; set; }
		public static GeneralLedgerEntryNumberPrefix Prefix { get; } = new("bads");

		public GeneralLedgerEntryNumber? ReferenceNumber =>
			SequenceNumber.HasValue ? new GeneralLedgerEntryNumber(Prefix, SequenceNumber.Value) : null;

		public IEnumerable<object> GetTransactionItems(ChartOfAccounts chartOfAccounts) {
			yield return new Credit(Account, new Money(1m));
		}

		public AccountNumber Account { get; set; }
	}
}
