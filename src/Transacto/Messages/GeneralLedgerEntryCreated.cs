using System;

namespace Transacto.Messages {
    public class GeneralLedgerEntryCreated {
        public Guid Id { get; set; }
        public string Number { get; set; }
        public DateTimeOffset CreatedOn { get; set; }
    }
}
