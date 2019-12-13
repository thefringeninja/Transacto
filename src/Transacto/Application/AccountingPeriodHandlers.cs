using System.Threading;
using System.Threading.Tasks;
using Transacto.Domain;
using Transacto.Messages;

namespace Transacto.Application {
    public class AccountingPeriodHandlers {
        private readonly IAccountingPeriodRepository _accountingPeriods;

        public AccountingPeriodHandlers(IAccountingPeriodRepository accountingPeriods) {
            _accountingPeriods = accountingPeriods;
        }

        public ValueTask Handle(OpenAccountingPeriod command, CancellationToken cancellationToken = default) {
            var accountingPeriod = AccountingPeriod.Open(PeriodIdentifier.FromDto(command.Period!));

            _accountingPeriods.Add(accountingPeriod);

            return default;
        }

        public async ValueTask Handle(CloseAccountingPeriod command, CancellationToken cancellationToken = default) {
            var accountingPeriod =
                await _accountingPeriods.Get(PeriodIdentifier.FromDto(command.Period!), cancellationToken);

            accountingPeriod.Close();
        }
    }
}
