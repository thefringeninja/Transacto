using System;
using System.Threading;
using System.Threading.Tasks;
using Transacto.Domain;
using Transacto.Messages;

namespace Transacto.Application {
    public class GeneralLedgerEntryHandlers {
        private readonly IGeneralLedgerEntryRepository _generalLedgerEntries;

        public GeneralLedgerEntryHandlers(IGeneralLedgerEntryRepository generalLedgerEntries) {
            if (generalLedgerEntries == null) throw new ArgumentNullException(nameof(generalLedgerEntries));
            _generalLedgerEntries = generalLedgerEntries;
        }

        public async ValueTask Handle(PostGeneralLedgerEntry command, CancellationToken cancellationToken = default) {
            var entry = await _generalLedgerEntries.Get(new GeneralLedgerEntryIdentifier(command.GeneralLedgerEntryId),
                cancellationToken);

            entry.Post();
        }
    }
}
