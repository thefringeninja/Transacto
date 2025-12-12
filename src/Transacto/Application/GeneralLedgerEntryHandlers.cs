using NodaTime;
using Transacto.Domain;
using Transacto.Messages;

namespace Transacto.Application;

public class GeneralLedgerEntryHandlers(
	IGeneralLedgerRepository ledger,
	IGeneralLedgerEntryRepository generalLedgerEntries,
	AccountIsDeactivated accountIsDeactivated) {
	public async ValueTask Handle(PostGeneralLedgerEntry command, CancellationToken cancellationToken = default) {
		var generalLedger = await ledger.Get(cancellationToken);

		var entry = generalLedger.Create(new GeneralLedgerEntryIdentifier(command.GeneralLedgerEntryId),
			command.BusinessTransaction,
			OffsetDateTime.FromDateTimeOffset(command.CreatedOn).LocalDateTime, accountIsDeactivated);

		entry.Post();

		generalLedgerEntries.Add(entry);
	}
}
