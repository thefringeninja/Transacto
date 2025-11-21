using System.Text;
using NodaTime;
using Transacto.Domain;
using Transacto.Messages;
using Transacto.Testing;

namespace Transacto;

public static class GeneralLedgerEntrySpecifications {
	public class PostingAnEntry(
		GeneralLedgerEntryIdentifier generalLedgerEntryIdentifier,
		GeneralLedgerEntrySequenceNumber sequenceNumber,
		LocalDate openedOn,
		AssetAccount assetAccount,
		LiabilityAccount liabilityAccount,
		Money amount) : EventSpecification(new Scenario()
		.Given(GeneralLedger.Identifier,
			new GeneralLedgerOpened {
				OpenedOn = Time.Format.LocalDate(openedOn)
			})
		.When(new PostGeneralLedgerEntry {
			GeneralLedgerEntryId = generalLedgerEntryIdentifier.ToGuid(),
			Period = AccountingPeriod.Open(openedOn).ToString(),
			BusinessTransaction = new TestTransaction(sequenceNumber,
				[new Debit(assetAccount.AccountNumber, amount)],
				[new Credit(liabilityAccount.AccountNumber, amount)]),
			CreatedOn = openedOn.ToDateTimeUnspecified()
		})
		.Then(GeneralLedgerEntry.FormatStreamIdentifier(generalLedgerEntryIdentifier),
			new GeneralLedgerEntryCreated {
				Period = AccountingPeriod.Open(openedOn).ToString(),
				CreatedOn = Time.Format.LocalDateTime(openedOn.AtMidnight()),
				ReferenceNumber = new GeneralLedgerEntryNumber(new("t"), sequenceNumber).ToString(),
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
			new TestTransaction(sequenceNumber,
				[new Debit(assetAccount.AccountNumber, amount)],
				[new Credit(liabilityAccount.AccountNumber, amount)]),
			new GeneralLedgerEntryPosted {
				Period = AccountingPeriod.Open(openedOn).ToString(),
				GeneralLedgerEntryId = generalLedgerEntryIdentifier.ToGuid()
			}));

	public class EntryDateNotInCurrentOrNextPeriodThrows(
		GeneralLedgerEntryIdentifier generalLedgerEntryIdentifier,
		GeneralLedgerEntrySequenceNumber sequenceNumber,
		LocalDate openedOn) : ExceptionSpecification(new Scenario()
		.Given(GeneralLedger.Identifier,
			new GeneralLedgerOpened {
				OpenedOn = Time.Format.LocalDate(openedOn)
			})
		.When(new PostGeneralLedgerEntry {
			GeneralLedgerEntryId = generalLedgerEntryIdentifier.ToGuid(),
			Period = AccountingPeriod.Open(openedOn).Next().Next().ToString(),
			BusinessTransaction = new TestTransaction(sequenceNumber),
			CreatedOn = openedOn.PlusMonths(2).AtMidnight().ToDateTimeUnspecified()
		})
		.Throws(new GeneralLedgerEntryNotInPeriodException(new(new("t"), sequenceNumber),
			openedOn.PlusMonths(2).AtMidnight(),
			AccountingPeriod.Open(openedOn).Next()))
	);

	public class ApplyingDebitToNonExistingAccountThrows(
		GeneralLedgerEntryIdentifier generalLedgerEntryIdentifier,
		GeneralLedgerEntryNumber referenceNumber,
		LocalDate openedOn,
		AccountNumber accountNumber) : ExceptionSpecification(new Scenario()
		.Given(GeneralLedger.Identifier,
			new GeneralLedgerOpened {
				OpenedOn = Time.Format.LocalDate(openedOn)
			})
		.When(new PostGeneralLedgerEntry {
			GeneralLedgerEntryId = generalLedgerEntryIdentifier.ToGuid(),
			Period = AccountingPeriod.Open(openedOn).ToString(),
			BusinessTransaction = new TestTransaction(referenceNumber.SequenceNumber,
				[new Debit(accountNumber, Money.Zero)]),
			CreatedOn = openedOn.ToDateTimeUnspecified()
		})
		.Throws(new AccountDeactivatedException(accountNumber)));

	public class ApplyingCreditToNonExistingAccountThrows(
		GeneralLedgerEntryIdentifier generalLedgerEntryIdentifier,
		GeneralLedgerEntrySequenceNumber sequenceNumber,
		LocalDate openedOn,
		AccountNumber accountNumber) : ExceptionSpecification(new Scenario()
		.Given(GeneralLedger.Identifier,
			new GeneralLedgerOpened {
				OpenedOn = Time.Format.LocalDate(openedOn)
			})
		.When(new PostGeneralLedgerEntry {
			GeneralLedgerEntryId = generalLedgerEntryIdentifier.ToGuid(),
			Period = AccountingPeriod.Open(openedOn).ToString(),
			BusinessTransaction = new TestTransaction(sequenceNumber,
				credits: [new Credit(accountNumber, Money.Zero)]),
			CreatedOn = openedOn.ToDateTimeUnspecified()
		})
		.Throws(new AccountDeactivatedException(accountNumber)));

	public class EntryNotInBalanceThrows(
		GeneralLedgerEntryIdentifier generalLedgerEntryIdentifier,
		GeneralLedgerEntrySequenceNumber sequenceNumber,
		LocalDate openedOn,
		Account account) : ExceptionSpecification(new Scenario()
		.Given(GeneralLedger.Identifier,
			new GeneralLedgerOpened {
				OpenedOn = Time.Format.LocalDate(openedOn)
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
				Account = account.AccountNumber,
				ReferenceNumber = sequenceNumber.ToInt32()
			},
			CreatedOn = openedOn.ToDateTimeUnspecified(),
			GeneralLedgerEntryId = generalLedgerEntryIdentifier.ToGuid()
		})
		.Throws(new GeneralLedgerEntryNotInBalanceException(generalLedgerEntryIdentifier)));

	public class TestTransaction(
		GeneralLedgerEntrySequenceNumber sequenceNumber,
		Debit[]? debits = null,
		Credit[]? credits = null)
		: IBusinessTransaction {
		private readonly Debit[] _debits = debits ?? [];
		private readonly Credit[] _credits = credits ?? [];
		public GeneralLedgerEntrySequenceNumber SequenceNumber { get; } = sequenceNumber;
		public IEnumerable<object> GetTransactionItems() => _credits.Cast<object>().Concat(_debits.Cast<object>());

		public override string ToString() => GetTransactionItems()
			.Aggregate(new StringBuilder("Test transaction ").Append(SequenceNumber).AppendLine(),
				(acc, e) => acc.Append('\t').Append(e).AppendLine())
			.ToString()
			.TrimEnd();
	}

	public class BadTransaction : IBusinessTransaction {
		public GeneralLedgerEntrySequenceNumber SequenceNumber => new(ReferenceNumber);

		public IEnumerable<object> GetTransactionItems() {
			yield return new Credit(Account, new Money(1m));
		}

		public int ReferenceNumber { get; set; }

		public AccountNumber Account { get; set; }
	}
}
