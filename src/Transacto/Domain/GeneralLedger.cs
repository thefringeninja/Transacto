using System;
using Transacto.Framework;
using Transacto.Messages;

namespace Transacto.Domain {
	public class GeneralLedger : AggregateRoot {
		public static readonly Func<GeneralLedger> Factory = () => new GeneralLedger();

		private PeriodIdentifier _period;

		public GeneralLedger(PeriodIdentifier period) : this() {
			_period = period;
		}

		private GeneralLedger() {
			Register<AccountingPeriodClosing>(e => _period = PeriodIdentifier.FromDto(e.Period));
			Register<AccountingPeriodClosed>(e => _period = PeriodIdentifier.FromDto(e.Period));
		}

		public GeneralLedgerEntry Create(GeneralLedgerEntryIdentifier identifier, GeneralLedgerEntryNumber number,
			DateTimeOffset createdOn) =>
			Create(identifier, number, createdOn, _period.Contains(createdOn) ? _period : _period.Next());

		private static GeneralLedgerEntry Create(GeneralLedgerEntryIdentifier identifier,
			GeneralLedgerEntryNumber number, DateTimeOffset createdOn, PeriodIdentifier period) =>
			period.Contains(createdOn)
				? new GeneralLedgerEntry(identifier, number, period, createdOn)
				: throw new InvalidOperationException();

		public void Close(GeneralLedgerEntryIdentifier[] generalLedgerEntryIdentifiers, Func<DateTimeOffset> clock) {
			Apply(new AccountingPeriodClosing {
				Period = _period.ToDto(),
				GeneralLedgerEntryIds = Array.ConvertAll(generalLedgerEntryIdentifiers, id => id.ToGuid())
			});
		}
	}
}
