using System;

namespace Transacto.Messages {
    public class GeneralLedgerEntryPosted {
        public Guid GeneralLedgerEntryId { get; set; }
        public string Period { get; set; } = null!;

        public override string ToString() => "General ledger entry was posted.";
    }
}
