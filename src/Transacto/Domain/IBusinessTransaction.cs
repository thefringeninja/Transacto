using System;
using System.Collections.Generic;

namespace Transacto.Domain {
    public interface IBusinessTransaction {
        public GeneralLedgerEntry GetGeneralLedgerEntry(PeriodIdentifier period, DateTimeOffset createdOn);
        public IEnumerable<object> Transaction { get; }
    }
}
