using System.Threading;
using System.Threading.Tasks;

namespace Transacto.Domain {
    public interface IGeneralLedgerEntryRepository {
        ValueTask<GeneralLedgerEntry> Get(GeneralLedgerEntryIdentifier identifier,
            CancellationToken cancellationToken = default);

        ValueTask<GeneralLedgerEntry[]> GetPosted(CancellationToken cancellationToken = default);

        void Add(GeneralLedgerEntry generalLedgerEntry);
    }
}
