using System;
using System.Linq;
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

		public async ValueTask<GeneralLedger> Get(CancellationToken cancellationToken = default) {
			var facts = await _factRecorder.GetFacts().Where(x => x.Identifier == GeneralLedger.Identifier)
				.ToArrayAsync(cancellationToken);

			if (facts.Length == 0) {
				throw new InvalidOperationException();
			}

			var generalLedger = GeneralLedger.Factory();
			await generalLedger.LoadFromHistory(facts.Select(x => x.Event).ToAsyncEnumerable());
			_factRecorder.Record(generalLedger.Id, generalLedger);
			return generalLedger;

		}

		public void Add(GeneralLedger generalLedger) =>
			_factRecorder.Record(generalLedger.Id, generalLedger.GetChanges());
	}
}
