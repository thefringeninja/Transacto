using System;

namespace Transacto.Messages {
    public record GeneralLedgerEntryCreated {
	    public Guid GeneralLedgerEntryId { get; init; }
        public string Number { get; init; } = default!;
        public string CreatedOn { get; init; } = default!;
        public string Period { get; init; } = default!;
        public override string ToString() => $"General ledger entry {Number} was created.";
    }
}
