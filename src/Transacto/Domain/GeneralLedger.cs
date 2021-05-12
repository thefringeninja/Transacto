using System;
using System.Linq;
using Transacto.Framework;
using Transacto.Messages;

namespace Transacto.Domain {
	public class GeneralLedger : AggregateRoot {
		public const string Identifier = "generalLedger";
		public static readonly Func<GeneralLedger> Factory = () => new GeneralLedger();

		private Period _period;
		private bool _periodClosing;

		public override string Id { get; } = Identifier;

		public static GeneralLedger Open(DateTimeOffset openedOn) {
			var generalLedger = new GeneralLedger();
			generalLedger.Apply(new GeneralLedgerOpened {
				OpenedOn = openedOn
			});
			return generalLedger;
		}

		private GeneralLedger() {
			Register<GeneralLedgerOpened>(e => _period = Period.Open(e.OpenedOn));
			Register<AccountingPeriodClosing>(e => {
				_periodClosing = true;
				_period = Period.Parse(e.Period);
			});
			Register<AccountingPeriodClosed>(e => {
				_period = Period.Parse(e.Period).Next();
				_periodClosing = false;
			});
		}

		public GeneralLedgerEntry Create(GeneralLedgerEntryIdentifier identifier, GeneralLedgerEntryNumber number,
			DateTimeOffset createdOn) => _periodClosing
			? Create(identifier, number, createdOn, _period.Next())
			: Create(identifier, number, createdOn, _period.Contains(createdOn) ? _period : _period.Next());

		private static GeneralLedgerEntry Create(GeneralLedgerEntryIdentifier identifier,
			GeneralLedgerEntryNumber number, DateTimeOffset createdOn, Period period) =>
			new(identifier, number, period, createdOn);

		public void BeginClosingPeriod(AccountNumber retainedEarningsAccountNumber,
			GeneralLedgerEntryIdentifier closingGeneralLedgerEntryIdentifier,
			GeneralLedgerEntryIdentifier[] generalLedgerEntryIdentifiers, DateTimeOffset closingOn) {
			if (_periodClosing) {
				throw new PeriodClosingInProcessException(_period);
			}

			AccountType.OfAccountNumber(retainedEarningsAccountNumber).MustBe(AccountType.Equity);

			_period.MustNotBeAfter(closingOn);

			Apply(new AccountingPeriodClosing {
				Period = _period.ToString(),
				GeneralLedgerEntryIds = Array.ConvertAll(generalLedgerEntryIdentifiers, id => id.ToGuid()),
				ClosingOn = closingOn,
				RetainedEarningsAccountNumber = retainedEarningsAccountNumber.ToInt32(),
				ClosingGeneralLedgerEntryId = closingGeneralLedgerEntryIdentifier.ToGuid()
			});
		}

		public void CompleteClosingPeriod(GeneralLedgerEntryIdentifier[] generalLedgerEntryIdentifiers,
			GeneralLedgerEntry closingEntry, TrialBalance trialBalance) {
			if (!_periodClosing) {
				throw new PeriodOpenException(_period);
			}

			foreach (var change in closingEntry.GetChanges()) {
				Apply(change);
			}

			trialBalance.Transfer(closingEntry);

			trialBalance.MustBeInBalance();

			Apply(new AccountingPeriodClosed {
				GeneralLedgerEntryIds =
					Array.ConvertAll(generalLedgerEntryIdentifiers, identifier => identifier.ToGuid()),
				ClosingGeneralLedgerEntryId = closingEntry.Identifier.ToGuid(),
				Period = _period.ToString(),
				Balance = trialBalance.Select(x => new BalanceLineItem {
					Amount = x.Value.ToDecimal(),
					AccountNumber = x.Key.ToInt32()
				}).ToArray()
			});
		}
	}
}
