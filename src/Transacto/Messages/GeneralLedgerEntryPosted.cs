using System;

namespace Transacto.Messages {
    public class GeneralLedgerEntryPosted {
        public Guid GeneralLedgerEntryId { get; set; }
        public string Period { get; set; } = null!;
    }
}
