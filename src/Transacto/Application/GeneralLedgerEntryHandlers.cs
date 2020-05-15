using System;
using System.Threading;
using System.Threading.Tasks;
using Transacto.Domain;
using Transacto.Messages;

namespace Transacto.Application {
    public class GeneralLedgerEntryHandlers {
        private readonly IGeneralLedgerEntryRepository _generalLedgerEntries;

        public GeneralLedgerEntryHandlers(IGeneralLedgerEntryRepository generalLedgerEntries) {
            _generalLedgerEntries = generalLedgerEntries;
        }

        public ValueTask Handle(PostGeneralLedgerEntry command, CancellationToken cancellationToken = default) {
	        if (command.BusinessTransaction == null) {
		        throw new Exception();
	        }
	        var entry = command.BusinessTransaction.GetGeneralLedgerEntry(PeriodIdentifier.FromDto(command.Period),
		        command.CreatedOn);

            entry.Post();

            _generalLedgerEntries.Add(entry);

            return new ValueTask(Task.CompletedTask);
        }
    }
}
