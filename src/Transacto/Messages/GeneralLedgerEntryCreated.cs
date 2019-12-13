using System;

namespace Transacto.Messages {
    public class GeneralLedgerEntryCreated {
        public Guid GeneralLedgerEntryId { get; set; }
        public string? Number { get; set; }
        public DateTimeOffset CreatedOn { get; set; }
        public PeriodDto? Period { get; set; }
    }
}
