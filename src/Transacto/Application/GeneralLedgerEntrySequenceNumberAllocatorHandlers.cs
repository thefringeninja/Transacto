using Transacto.Domain;
using Transacto.Messages;

namespace Transacto.Application;

public class GeneralLedgerEntrySequenceNumberAllocatorHandlers(
	IGeneralLedgerEntryRepository generalLedgerEntries,
	GetSequenceNumbers getSequenceNumbers) {
	public async ValueTask Handle(GeneralLedgerEntryPosted @event, CancellationToken cancellationToken) {
		var generalLedgerEntryIdentifier = new GeneralLedgerEntryIdentifier(@event.GeneralLedgerEntryId);
		var optional = await generalLedgerEntries.GetOptional(generalLedgerEntryIdentifier, cancellationToken);

		if (!optional.HasValue) {
			return;
		}

		var generalLedgerEntry = optional.Value;

		if (generalLedgerEntry.BusinessTransaction is not { } businessTransaction) {
			return;
		}

		if (generalLedgerEntry.SequenceNumber.HasValue) {
			return;
		}

		var sequenceNumbers = getSequenceNumbers(businessTransaction);

		var sequenceNumber = await sequenceNumbers.NextAsync(generalLedgerEntry, cancellationToken);
	}

	public async ValueTask Handle(GeneralLedgerEntryNumberAllocated @event, CancellationToken cancellationToken) {
		var optional =
			await generalLedgerEntries.GetOptional(new GeneralLedgerEntryIdentifier(@event.GeneralLedgerEntryId),
				cancellationToken);

		if (!optional.HasValue) {
			return;
		}

		var generalLedgerEntry = optional.Value;

		generalLedgerEntry.AssignSequenceNumber(
			new GeneralLedgerEntrySequenceNumber(@event.GeneralLedgerEntrySequenceNumber));
	}
}
