using System;

namespace Transacto.Messages {
    public record GeneralLedgerEntryCreated {
	    public Guid GeneralLedgerEntryId;
        public string Number = default!;
        public DateTimeOffset CreatedOn;
        public string Period = default!;
        public override string ToString() => $"General ledger entry {Number} was created.";
    }
}
