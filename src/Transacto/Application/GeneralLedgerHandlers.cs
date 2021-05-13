using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NodaTime;
using Transacto.Domain;
using Transacto.Messages;

namespace Transacto.Application {
	public class GeneralLedgerHandlers {
		private readonly IGeneralLedgerRepository _generalLedger;

		public GeneralLedgerHandlers(IGeneralLedgerRepository generalLedger) {
			_generalLedger = generalLedger;
		}

		public ValueTask Handle(OpenGeneralLedger command, CancellationToken cancellationToken = default) {
			_generalLedger.Add(GeneralLedger.Open(LocalDate.FromDateTime(command.OpenedOn.LocalDateTime)));

			return new ValueTask(Task.CompletedTask);
		}

		public async ValueTask Handle(BeginClosingAccountingPeriod command,
			CancellationToken cancellationToken = default) {
			var generalLedger = await _generalLedger.Get(cancellationToken);

			generalLedger.BeginClosingPeriod(new AccountNumber(command.RetainedEarningsAccountNumber),
				new GeneralLedgerEntryIdentifier(command.ClosingGeneralLedgerEntryId),
				command.GeneralLedgerEntryIds.Select(id => new GeneralLedgerEntryIdentifier(id)).ToArray(),
				LocalDateTime.FromDateTime(command.ClosingOn.DateTime));
		}
	}
}
