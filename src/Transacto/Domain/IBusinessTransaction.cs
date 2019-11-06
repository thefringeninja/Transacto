using System.Collections.Generic;

namespace Transacto.Domain {
    public interface IBusinessTransaction {
        public GeneralLedgerEntry GetGeneralLedgerEntry();
        public IEnumerable<object> Transaction { get; }
    }
}
