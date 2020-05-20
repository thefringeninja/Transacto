using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Transacto.Domain;
using Transacto.Messages;
using Transacto.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Transacto.Application {
	public class GeneralLedgerEntryTests {
		private readonly GeneralLedgerEntryHandlers _handler;
		private readonly IFactRecorder _facts;
		private readonly TestSpecificationTextWriter _writer;

		public GeneralLedgerEntryTests(ITestOutputHelper output) {
			_writer = new TestSpecificationTextWriter(new TestOutputHelperTextWriter(output));
			_facts = new FactRecorder();
			_handler = new GeneralLedgerEntryHandlers(new GeneralLedgerTestRepository(_facts),
				new GeneralLedgerEntryTestRepository(_facts),
				_ => false);
		}

		[Theory, AutoTransactoData]
		public Task entry_not_in_balance_throws(GeneralLedgerEntryIdentifier generalLedgerEntryIdentifier,
			int sequenceNumber, DateTimeOffset openedOn, AccountName accountName, AccountNumber accountNumber) {
			var scenario = new Scenario()
				.Given("generalLedger",
					new GeneralLedgerOpened {
						OpenedOn = openedOn
					})
				.Given("chartOfAccounts",
					new AccountDefined {
						AccountName = accountName.ToString(),
						AccountNumber = accountNumber.ToInt32()
					})
				.When(new PostGeneralLedgerEntry {
					Period = Period.Open(openedOn).ToString(),
					BusinessTransaction = new BadTransaction {
						Account = accountNumber,
						ReferenceNumber = sequenceNumber
					},
					CreatedOn = openedOn,
					GeneralLedgerEntryId = generalLedgerEntryIdentifier.ToGuid()
				})
				.Throws(new GeneralLedgerEntryNotInBalanceException(generalLedgerEntryIdentifier));
			_writer.Write(scenario.Build());
			return scenario.Assert(_handler, _facts);
		}

		private class BadTransaction : IBusinessTransaction {
			GeneralLedgerEntryNumber IBusinessTransaction.ReferenceNumber =>
				new GeneralLedgerEntryNumber("BAD", ReferenceNumber);

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
