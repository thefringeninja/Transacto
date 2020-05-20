using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Transacto.Domain;
using Transacto.Messages;

namespace Transacto.Plugins.GeneralLedger {
	internal class GeneralLedger : IPlugin {
		public string Name { get; } = nameof(GeneralLedger);

		public void Configure(IEndpointRouteBuilder builder) => builder
			.MapBusinessTransaction<JournalEntry>("/entries")
			.MapCommands(string.Empty,
				typeof(OpenGeneralLedger),
				typeof(BeginClosingAccountingPeriod));

		public void ConfigureServices(IServiceCollection services) => services
			.AddInMemoryProjection(new InMemoryProjectionBuilder()
				.When<AccountDeactivated>((readModel, e) => readModel.AddOrUpdate("DeactivatedAccounts",
					deactivatedAccounts => deactivatedAccounts.Add(e.Message.AccountNumber), () => new HashSet<int>()))
				.When<AccountReactivated>((readModel, e) => readModel.AddOrUpdate("DeactivatedAccounts",
					deactivatedAccounts => deactivatedAccounts.Remove(e.Message.AccountNumber),
					() => new HashSet<int>()))
				.Build())
			.AddInMemoryProjection(new InMemoryProjectionBuilder()
				.When<GeneralLedgerEntryPosted>((readModel, e) =>
					readModel.AddOrUpdate(e.Message.Period,
						l => l.Add(e.Message.GeneralLedgerEntryId),
						() => new List<Guid> {e.Message.GeneralLedgerEntryId}))
				.When<AccountingPeriodClosed>((readModel, e) => {
					if (!readModel.TryRemove<List<Guid>>(e.Message.Period, out var value)) {
						return;
					}

					var notClosed = value.Except(e.Message.GeneralLedgerEntryIds)
						.Except(new[] {e.Message.ClosingGeneralLedgerEntryId})
						.ToList();

					if (notClosed.Count == 0) {
						return;
					}

					readModel.AddOrUpdate(nameof(notClosed), x => x.AddRange(notClosed), () => notClosed);
				})
				.Build());

		public IEnumerable<Type> MessageTypes => Enumerable.Empty<Type>();
	}
}
