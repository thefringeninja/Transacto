using System;
using System.Threading;
using System.Threading.Tasks;
using Transacto.Domain;
using Transacto.Messages;

namespace Transacto.Application {
	public class GeneralLedgerEntryHandlers {
		private readonly IGeneralLedgerRepository _generalLedger;
		private readonly IGeneralLedgerEntryRepository _generalLedgerEntries;

		public GeneralLedgerEntryHandlers(IGeneralLedgerRepository generalLedger,
			IGeneralLedgerEntryRepository generalLedgerEntries) {
			_generalLedger = generalLedger;
			_generalLedgerEntries = generalLedgerEntries;
		}

		public async ValueTask Handle(PostGeneralLedgerEntry command, CancellationToken cancellationToken = default) {
			if (command.BusinessTransaction == null) {
				throw new Exception();
			}

			var generalLedger = await _generalLedger.Get(cancellationToken);
			var entry = generalLedger.Create(new GeneralLedgerEntryIdentifier(command.GeneralLedgerEntryId),
				command.BusinessTransaction.ReferenceNumber, command.CreatedOn);
			command.BusinessTransaction.Apply(entry);

			entry.Post();

			_generalLedgerEntries.Add(entry);
		}
	}
}
