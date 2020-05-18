using System;
using System.Threading;
using System.Threading.Tasks;
using Transacto.Domain;
using Transacto.Messages;

namespace Transacto.Application {
	public class GeneralLedgerHandlers {
		private readonly IGeneralLedgerRepository _generalLedger;

		public GeneralLedgerHandlers(IGeneralLedgerRepository generalLedger) {
			_generalLedger = generalLedger;
		}

		public async ValueTask Handle(CloseAccountingPeriod command, CancellationToken cancellationToken = default) {
			var generalLedger = await _generalLedger.Get(cancellationToken);

			generalLedger.Close(Array.ConvertAll(command.GeneralLedgerEntryIds,
				id => new GeneralLedgerEntryIdentifier(id)), () => DateTimeOffset.Now);
		}
	}
}
