using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Transacto.Domain;
using Transacto.Messages;

namespace Transacto.Application {
	public class GeneralLedgerHandlers {
		private readonly IGeneralLedgerRepository _generalLedger;

		public GeneralLedgerHandlers(IGeneralLedgerRepository generalLedger) {
			_generalLedger = generalLedger;
		}

		public ValueTask Handle(OpenGeneralLedger command, CancellationToken cancellationToken = default) {
			_generalLedger.Add(GeneralLedger.Open(command.OpenedOn));

			return new ValueTask(Task.CompletedTask);
		}

		public async ValueTask Handle(BeginClosingAccountingPeriod command,
			CancellationToken cancellationToken = default) {
			var generalLedger = await _generalLedger.Get(cancellationToken);

			var retainedEarningsAccountNumber = new AccountNumber(command.RetainedEarningsAccountNumber);
			AccountType.OfAccountNumber(retainedEarningsAccountNumber).MustBe(AccountType.Equity);

			generalLedger.BeginClosingPeriod(retainedEarningsAccountNumber,
				new GeneralLedgerEntryIdentifier(command.ClosingGeneralLedgerEntryId),
				command.GeneralLedgerEntryIds.Select(id => new GeneralLedgerEntryIdentifier(id)).ToArray(),
				command.ClosingOn);
		}
	}
}
