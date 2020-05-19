using System;

namespace Transacto.Messages {
    public class GeneralLedgerEntryCreated {
        public Guid GeneralLedgerEntryId { get; set; }
        public string Number { get; set; } = null!;
        public DateTimeOffset CreatedOn { get; set; }
        public string Period { get; set; } = null!;
    }
}
