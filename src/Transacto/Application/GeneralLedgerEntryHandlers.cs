using NodaTime;
using Transacto.Domain;
using Transacto.Messages;

namespace Transacto.Application; 

public class GeneralLedgerEntryHandlers {
	private readonly IGeneralLedgerRepository _generalLedger;
	private readonly IGeneralLedgerEntryRepository _generalLedgerEntries;
	private readonly GetPrefix _getPrefix;
	private readonly AccountIsDeactivated _accountIsDeactivated;

	public GeneralLedgerEntryHandlers(IGeneralLedgerRepository generalLedger,
		IGeneralLedgerEntryRepository generalLedgerEntries, GetPrefix getPrefix,
		AccountIsDeactivated accountIsDeactivated) {
		_generalLedger = generalLedger;
		_generalLedgerEntries = generalLedgerEntries;
		_getPrefix = getPrefix;
		_accountIsDeactivated = accountIsDeactivated;
	}

	public async ValueTask Handle(PostGeneralLedgerEntry command, CancellationToken cancellationToken = default) {
		var generalLedger = await _generalLedger.Get(cancellationToken);

		if (command.BusinessTransaction == null) {
			throw new NotImplementedException();
		}

		var entry = generalLedger.Create(new GeneralLedgerEntryIdentifier(command.GeneralLedgerEntryId),
			command.BusinessTransaction, _getPrefix(command.BusinessTransaction),
			OffsetDateTime.FromDateTimeOffset(command.CreatedOn).LocalDateTime, _accountIsDeactivated);

		entry.Post();

		_generalLedgerEntries.Add(entry);
	}
}
