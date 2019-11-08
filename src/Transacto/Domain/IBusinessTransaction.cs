using System;
using System.Collections.Generic;

namespace Transacto.Domain {
    public interface IBusinessTransaction {
        public GeneralLedgerEntry GetGeneralLedgerEntry(PeriodIdentifier period, DateTimeOffset createdOn);
        IEnumerable<object> GetAdditionalChanges();
    }
}
