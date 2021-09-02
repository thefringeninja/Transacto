using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Transacto.Domain;
using Transacto.Framework.Projections;
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
			.AddCommandHandlerModule<GeneralLedgerEntryModule>()
			.AddCommandHandlerModule<GeneralLedgerModule>()
			.AddSingleton<AccountIsDeactivated>(provider => accountNumber => {
				var readModel = provider.GetRequiredService<InMemoryProjectionDatabase>().Get<DeactivatedAccounts>();
				return readModel.HasValue && readModel.Value.AccountNumbers.Contains(accountNumber.ToInt32());
			})
			.AddInMemoryProjection(new InMemoryProjectionBuilder<DeactivatedAccounts>()
				.When((readModel, e) => readModel with {
					AccountNumbers = e switch {
						AccountDeactivated d => readModel.AccountNumbers.Remove(d.AccountNumber),
						AccountReactivated r => readModel.AccountNumbers.Add(r.AccountNumber),
						_ => readModel.AccountNumbers
					}
				})
				.Build())
			.AddInMemoryProjection(new InMemoryProjectionBuilder<UnclosedGeneralLedgerEntries>()
				.When((readModel, e) => readModel with {
					EntriesByPeriod = e switch {
						GeneralLedgerEntryPosted p => readModel.EntriesByPeriod.SetItem(p.Period,
							(readModel.EntriesByPeriod.TryGetValue(p.Period, out var entries)
								? entries
								: ImmutableHashSet<Guid>.Empty)
							.Add(p.GeneralLedgerEntryId)),
						AccountingPeriodClosed c => readModel.EntriesByPeriod.Remove(c.Period),
						_ => readModel.EntriesByPeriod
					},
					NotClosed = e switch {
						AccountingPeriodClosed c => readModel.NotClosed
							.Except(c.GeneralLedgerEntryIds)
							.Remove(c.ClosingGeneralLedgerEntryId)
							.Compact(),
						_ => readModel.NotClosed
					}
				})
				.Build())
			.AddProcessManager<AccountingPeriodClosingModule>("accountingPeriodClosingCheckpoint");

		public IEnumerable<Type> MessageTypes => Enumerable.Empty<Type>();

		private record DeactivatedAccounts : MemoryReadModel {
			public ImmutableHashSet<int> AccountNumbers { get; init; } = ImmutableHashSet<int>.Empty;
		}

		private record UnclosedGeneralLedgerEntries : MemoryReadModel {
			public ImmutableHashSet<Guid> NotClosed { get; init; } = ImmutableHashSet<Guid>.Empty;

			public ImmutableDictionary<string, ImmutableHashSet<Guid>> EntriesByPeriod { get; init; } =
				ImmutableDictionary<string, ImmutableHashSet<Guid>>.Empty;
		}
	}
}
