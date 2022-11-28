using NodaTime;
using Serilog;
using Transacto.Domain;
using Transacto.Messages;
using Transacto.Testing;

namespace Transacto.Application;

public class GeneralLedgerEntryTests {
	private readonly IFactRecorder _facts;
	private readonly TestSpecificationTextWriter _writer;
	private readonly GetPrefix _getPrefix;

	public GeneralLedgerEntryTests() {
		_writer = new TestSpecificationTextWriter(new SerilogTextWriter(Log.Logger));
		_facts = new FactRecorder();
		_getPrefix = t => new GeneralLedgerEntryNumberPrefix(t.GetType().Name switch {
			nameof(BadTransaction) => "bad",
			nameof(TestTransaction) => "t",
			_ => "x"
		});
	}

	[AutoFixtureData]
	public Task posting_an_entry(GeneralLedgerEntryIdentifier generalLedgerEntryIdentifier,
		GeneralLedgerEntrySequenceNumber sequenceNumber, LocalDate openedOn, AssetAccount assetAccount,
		LiabilityAccount liabilityAccount, Money amount) =>
		new Scenario()
			.Given(GeneralLedger.Identifier,
				new GeneralLedgerOpened {
					OpenedOn = Time.Format.LocalDate(openedOn)
				})
			.When(new PostGeneralLedgerEntry {
				GeneralLedgerEntryId = generalLedgerEntryIdentifier.ToGuid(),
				Period = AccountingPeriod.Open(openedOn).ToString(),
				BusinessTransaction = new TestTransaction(sequenceNumber,
					new[] { new Debit(assetAccount.AccountNumber, amount) },
					new[] { new Credit(liabilityAccount.AccountNumber, amount) }),
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
					new[] { new Debit(assetAccount.AccountNumber, amount) },
					new[] { new Credit(liabilityAccount.AccountNumber, amount) }),
				new GeneralLedgerEntryPosted {
					Period = AccountingPeriod.Open(openedOn).ToString(),
					GeneralLedgerEntryId = generalLedgerEntryIdentifier.ToGuid()
				})
			.Assert(new GeneralLedgerEntryHandlers(new GeneralLedgerTestRepository(_facts),
				new GeneralLedgerEntryTestRepository(_facts), _getPrefix, _ => false), _facts);

	[AutoFixtureData]
	public Task entry_date_not_in_current_or_next_period_throws(
		GeneralLedgerEntryIdentifier generalLedgerEntryIdentifier,
		GeneralLedgerEntrySequenceNumber sequenceNumber, LocalDate openedOn) {
		var createdOn = openedOn.PlusMonths(2).AtMidnight();
		return new Scenario()
			.Given(GeneralLedger.Identifier,
				new GeneralLedgerOpened {
					OpenedOn = Time.Format.LocalDate(openedOn)
				})
			.When(new PostGeneralLedgerEntry {
				GeneralLedgerEntryId = generalLedgerEntryIdentifier.ToGuid(),
				Period = AccountingPeriod.Open(openedOn).Next().Next().ToString(),
				BusinessTransaction = new TestTransaction(sequenceNumber),
				CreatedOn = createdOn.ToDateTimeUnspecified()
			})
			.Throws(new GeneralLedgerEntryNotInPeriodException(new(new("t"), sequenceNumber), createdOn,
				AccountingPeriod.Open(openedOn).Next()))
			.Assert(new GeneralLedgerEntryHandlers(new GeneralLedgerTestRepository(_facts),
				new GeneralLedgerEntryTestRepository(_facts), _getPrefix, _ => false), _facts);
	}

	[AutoFixtureData]
	public Task applying_debit_to_non_existing_account_throws(
		GeneralLedgerEntryIdentifier generalLedgerEntryIdentifier, GeneralLedgerEntryNumber referenceNumber,
		LocalDate openedOn, AccountNumber accountNumber) => new Scenario()
		.Given(GeneralLedger.Identifier,
			new GeneralLedgerOpened {
				OpenedOn = Time.Format.LocalDate(openedOn)
			})
		.When(new PostGeneralLedgerEntry {
			GeneralLedgerEntryId = generalLedgerEntryIdentifier.ToGuid(),
			Period = AccountingPeriod.Open(openedOn).ToString(),
			BusinessTransaction = new TestTransaction(referenceNumber.SequenceNumber,
				new[] { new Debit(accountNumber, Money.Zero) }),
			CreatedOn = openedOn.ToDateTimeUnspecified()
		})
		.Throws(new AccountDeactivatedException(accountNumber))
		.Assert(new GeneralLedgerEntryHandlers(new GeneralLedgerTestRepository(_facts),
			new GeneralLedgerEntryTestRepository(_facts), _getPrefix, _ => true), _facts);

	[AutoFixtureData]
	public Task applying_credit_to_non_existing_account_throws(
		GeneralLedgerEntryIdentifier generalLedgerEntryIdentifier,
		GeneralLedgerEntrySequenceNumber sequenceNumber, LocalDate openedOn, AccountNumber accountNumber) =>
		new Scenario()
			.Given(GeneralLedger.Identifier,
				new GeneralLedgerOpened {
					OpenedOn = Time.Format.LocalDate(openedOn)
				})
			.When(new PostGeneralLedgerEntry {
				GeneralLedgerEntryId = generalLedgerEntryIdentifier.ToGuid(),
				Period = AccountingPeriod.Open(openedOn).ToString(),
				BusinessTransaction = new TestTransaction(sequenceNumber,
					credits: new[] { new Credit(accountNumber, Money.Zero) }),
				CreatedOn = openedOn.ToDateTimeUnspecified()
			})
			.Throws(new AccountDeactivatedException(accountNumber))
			.Assert(new GeneralLedgerEntryHandlers(new GeneralLedgerTestRepository(_facts),
				new GeneralLedgerEntryTestRepository(_facts), _getPrefix, _ => true), _facts);

	[AutoFixtureData]
	public Task entry_not_in_balance_throws(GeneralLedgerEntryIdentifier generalLedgerEntryIdentifier,
		GeneralLedgerEntrySequenceNumber sequenceNumber, LocalDate openedOn, AccountName accountName,
		AccountNumber accountNumber) {
		var scenario = new Scenario()
			.Given(GeneralLedger.Identifier,
				new GeneralLedgerOpened {
					OpenedOn = Time.Format.LocalDate(openedOn)
				})
			.Given(ChartOfAccounts.Identifier,
				new AccountDefined {
					AccountName = accountName.ToString(),
					AccountNumber = accountNumber.ToInt32()
				})
			.When(new PostGeneralLedgerEntry {
				Period = AccountingPeriod.Open(openedOn).ToString(),
				BusinessTransaction = new BadTransaction {
					Account = accountNumber,
					ReferenceNumber = sequenceNumber.ToInt32()
				},
				CreatedOn = openedOn.ToDateTimeUnspecified(),
				GeneralLedgerEntryId = generalLedgerEntryIdentifier.ToGuid()
			})
			.Throws(new GeneralLedgerEntryNotInBalanceException(generalLedgerEntryIdentifier));
		_writer.Write(scenario.Build());
		return scenario.Assert(new GeneralLedgerEntryHandlers(new GeneralLedgerTestRepository(_facts),
			new GeneralLedgerEntryTestRepository(_facts), _getPrefix, _ => false), _facts);
	}

	private class TestTransaction : IBusinessTransaction {
		private readonly Debit[] _debits;
		private readonly Credit[] _credits;
		public GeneralLedgerEntrySequenceNumber SequenceNumber { get; }
		public IEnumerable<object> GetTransactionItems() => _credits.Cast<object>().Concat(_debits.Cast<object>());

		public TestTransaction(GeneralLedgerEntrySequenceNumber sequenceNumber, Debit[]? debits = null,
			Credit[]? credits = null) {
			SequenceNumber = sequenceNumber;
			_debits = debits ?? Array.Empty<Debit>();
			_credits = credits ?? Array.Empty<Credit>();
		}
	}

	private class BadTransaction : IBusinessTransaction {
		public GeneralLedgerEntrySequenceNumber SequenceNumber => new(ReferenceNumber);

		public IEnumerable<object> GetTransactionItems() {
			yield return new Credit(Account, new Money(1m));
		}

		public int ReferenceNumber { get; set; }

		public AccountNumber Account { get; set; }
	}
}
