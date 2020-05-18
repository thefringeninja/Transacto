using System.Threading;
using System.Threading.Tasks;
using Transacto.Domain;
using Transacto.Testing;

namespace Transacto.Application {
    internal class GeneralLedgerTestRepository : IGeneralLedgerRepository {
        private readonly IFactRecorder _factRecorder;

        public GeneralLedgerTestRepository(IFactRecorder factRecorder) {
            _factRecorder = factRecorder;
        }

        public ValueTask<GeneralLedger> Get(CancellationToken cancellationToken = default) {
	        throw new System.NotImplementedException();
        }
    }
}
