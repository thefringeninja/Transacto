using System;
using Transacto.Domain;

namespace Transacto.Messages {
    public class PostGeneralLedgerEntry {
        public PeriodDto Period { get; set; } = null!;
        public DateTimeOffset CreatedOn { get; set; }
        public IBusinessTransaction? BusinessTransaction { get; set; }
    }
}
