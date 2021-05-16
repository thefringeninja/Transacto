using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NodaTime;
using Transacto.Domain;
using Transacto.Messages;
using Transacto.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Transacto.Application {
	public class GeneralLedgerEntryTests {
		private readonly IFactRecorder _facts;
		private readonly TestSpecificationTextWriter _writer;

		public GeneralLedgerEntryTests(ITestOutputHelper output) {
			_writer = new TestSpecificationTextWriter(new TestOutputHelperTextWriter(output));
			_facts = new FactRecorder();
		}

		[Theory, AutoTransactoData]
		public Task entry_date_not_in_current_or_next_period_throws(
			GeneralLedgerEntryIdentifier generalLedgerEntryIdentifier, GeneralLedgerEntryNumber generalLedgerEntryNumber,
			LocalDate openedOn) {
			var createdOn = openedOn.PlusMonths(2).AtMidnight();
			return new Scenario()
				.Given(GeneralLedger.Identifier,
					new GeneralLedgerOpened {
						OpenedOn = Time.Format.LocalDate(openedOn)
					})
				.When(new PostGeneralLedgerEntry {
					GeneralLedgerEntryId = generalLedgerEntryIdentifier.ToGuid(),
					Period = AccountingPeriod.Open(openedOn).Next().Next().ToString(),
					BusinessTransaction = new TestTransaction(generalLedgerEntryNumber),
					CreatedOn = createdOn.ToDateTimeUnspecified()
				})
				.Throws(new GeneralLedgerEntryNotInPeriodException(generalLedgerEntryNumber, createdOn,
					AccountingPeriod.Open(openedOn).Next()))
				.Assert(new GeneralLedgerEntryHandlers(new GeneralLedgerTestRepository(_facts),
					new GeneralLedgerEntryTestRepository(_facts), _ => false), _facts);
		}

		[Theory, AutoTransactoData]
		public Task applying_debit_to_non_existing_account_throws(
			GeneralLedgerEntryIdentifier generalLedgerEntryIdentifier, int sequenceNumber, LocalDate openedOn,
			AccountNumber accountNumber) => new Scenario()
			.Given(GeneralLedger.Identifier,
				new GeneralLedgerOpened {
					OpenedOn = Time.Format.LocalDate(openedOn)
				})
			.When(new PostGeneralLedgerEntry {
				GeneralLedgerEntryId = generalLedgerEntryIdentifier.ToGuid(),
				Period = AccountingPeriod.Open(openedOn).ToString(),
				BusinessTransaction = new TestTransaction(new GeneralLedgerEntryNumber("t", sequenceNumber),
					new[]{new Debit(accountNumber, Money.Zero)}),
				CreatedOn = openedOn.ToDateTimeUnspecified()
			})
			.Throws(new AccountDeactivatedException(accountNumber))
			.Assert(new GeneralLedgerEntryHandlers(new GeneralLedgerTestRepository(_facts),
				new GeneralLedgerEntryTestRepository(_facts), _ => true), _facts);

		[Theory, AutoTransactoData]
		public Task applying_credit_to_non_existing_account_throws(
			GeneralLedgerEntryIdentifier generalLedgerEntryIdentifier, int sequenceNumber, LocalDate openedOn,
			AccountNumber accountNumber) => new Scenario()
			.Given(GeneralLedger.Identifier,
				new GeneralLedgerOpened {
					OpenedOn = Time.Format.LocalDate(openedOn)
				})
			.When(new PostGeneralLedgerEntry {
				GeneralLedgerEntryId = generalLedgerEntryIdentifier.ToGuid(),
				Period = AccountingPeriod.Open(openedOn).ToString(),
				BusinessTransaction = new TestTransaction(new GeneralLedgerEntryNumber("t", sequenceNumber),
					credits: new[]{new Credit(accountNumber, Money.Zero)}),
				CreatedOn = openedOn.ToDateTimeUnspecified()
			})
			.Throws(new AccountDeactivatedException(accountNumber))
			.Assert(new GeneralLedgerEntryHandlers(new GeneralLedgerTestRepository(_facts),
				new GeneralLedgerEntryTestRepository(_facts), _ => true), _facts);

		[Theory, AutoTransactoData]
		public Task entry_not_in_balance_throws(GeneralLedgerEntryIdentifier generalLedgerEntryIdentifier,
			int sequenceNumber, LocalDate openedOn, AccountName accountName, AccountNumber accountNumber) {
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
						ReferenceNumber = sequenceNumber
					},
					CreatedOn = openedOn.ToDateTimeUnspecified(),
					GeneralLedgerEntryId = generalLedgerEntryIdentifier.ToGuid()
				})
				.Throws(new GeneralLedgerEntryNotInBalanceException(generalLedgerEntryIdentifier));
			_writer.Write(scenario.Build());
			return scenario.Assert(new GeneralLedgerEntryHandlers(new GeneralLedgerTestRepository(_facts),
				new GeneralLedgerEntryTestRepository(_facts),
				_ => false), _facts);
		}
		
		private class TestTransaction : IBusinessTransaction {
			private readonly Debit[] _debits;
			private readonly Credit[] _credits;
			public GeneralLedgerEntryNumber ReferenceNumber { get; }

			public TestTransaction(GeneralLedgerEntryNumber referenceNumber, Debit[]? debits = null,
				Credit[]? credits = null) {
				ReferenceNumber = referenceNumber;
				_debits = debits ?? Array.Empty<Debit>();
				_credits = credits ?? Array.Empty<Credit>();
			}
			public void Apply(GeneralLedgerEntry generalLedgerEntry, AccountIsDeactivated accountIsDeactivated) {
				foreach (var credit in _credits) {
					generalLedgerEntry.ApplyCredit(credit, accountIsDeactivated);
				}

				foreach (var debit in _debits) {
					generalLedgerEntry.ApplyDebit(debit, accountIsDeactivated);
				}
			}

			public IEnumerable<object> GetAdditionalChanges() => Enumerable.Empty<object>();

			public int? Version { get; set; }
		}

		private class BadTransaction : IBusinessTransaction {
			GeneralLedgerEntryNumber IBusinessTransaction.ReferenceNumber =>
				new("BAD", ReferenceNumber);

			public int ReferenceNumber { get; set; }

			public AccountNumber Account { get; set; }

			public void Apply(GeneralLedgerEntry generalLedgerEntry, AccountIsDeactivated accountIsDeactivated) {
				generalLedgerEntry.ApplyCredit(new Credit(Account, new Money(1m)), accountIsDeactivated);
			}

			public IEnumerable<object> GetAdditionalChanges() {
				yield break;
			}

			public int? Version { get; set; }
		}
	}
}
