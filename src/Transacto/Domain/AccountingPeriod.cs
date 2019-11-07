using System;
using Transacto.Framework;
using Transacto.Messages;

namespace Transacto.Domain {
    public class AccountingPeriod : AggregateRoot {
        public static readonly Func<AccountingPeriod> Factory = () => new AccountingPeriod();

        private bool _closed;
        private PeriodIdentifier _period;
        public PeriodIdentifier Period => _period;

        private AccountingPeriod() {
            Register<AccountingPeriodOpened>(e => _period = PeriodIdentifier.FromDto(e.Period));
            Register<AccountingPeriodClosed>(_ => _closed = true);
        }

        public static AccountingPeriod Open(PeriodIdentifier period) {
            if (period == PeriodIdentifier.Empty) {
                throw new ArgumentOutOfRangeException(nameof(period));
            }

            var accountingPeriod = Factory();

            accountingPeriod.Apply(new AccountingPeriodOpened {
                Period = period.ToDto()
            });

            return accountingPeriod;
        }

        public void Close() {
            if (_closed) return;

            Apply(new AccountingPeriodClosed {
                Period = _period.ToDto()
            });
        }
    }
}
