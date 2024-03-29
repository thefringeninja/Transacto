using System.Collections.Concurrent;
using System.Collections.Immutable;
using Transacto.Domain;
using Transacto.Framework.Projections;
using Transacto.Messages;

namespace Transacto.Plugins.GeneralLedger;

internal class GeneralLedger : IPlugin {
	public string Name { get; } = nameof(GeneralLedger);
	public IEnumerable<Type> MessageTypes => Enumerable.Empty<Type>();

	public void Configure(IEndpointRouteBuilder builder) => builder
		.MapBusinessTransaction<JournalEntry>("/entries")
		.MapCommands(string.Empty,
			typeof(OpenGeneralLedger),
			typeof(BeginClosingAccountingPeriod));

	public void ConfigureServices(IServiceCollection services) => services
		.AddCommandHandlerModule<GeneralLedgerEntryModule>()
		.AddCommandHandlerModule<GeneralLedgerModule>()
		.AddSingleton<AccountIsDeactivated>(provider => accountNumber => provider
			.GetRequiredService<InMemoryProjectionDatabase>()
			.Get<DeactivatedAccounts>() switch {
			{ HasValue: true } optional => optional.Value.AccountNumbers.Contains(
				accountNumber.ToInt32()),
			_ => false
		})
		.AddSingleton<GetPrefix>(_ => {
			var cache = new ConcurrentDictionary<Type, string>();
			return t => new GeneralLedgerEntryNumberPrefix(
				cache.GetOrAdd(t.GetType(), type => char.ToLower(type.Name[0]) + type.Name[1..]));
		})
		.AddInMemoryProjection<DeactivatedAccounts>((readModel, e) => readModel with {
			AccountNumbers = e switch {
				AccountDeactivated d => readModel.AccountNumbers.Remove(d.AccountNumber),
				AccountReactivated r => readModel.AccountNumbers.Add(r.AccountNumber),
				_ => readModel.AccountNumbers
			}
		})
		.AddInMemoryProjection<UnclosedGeneralLedgerEntries>((readModel, e) => readModel with {
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
		.AddProcessManager<AccountingPeriodClosingModule>("accountingPeriodClosingCheckpoint");

	private record DeactivatedAccounts : MemoryReadModel {
		public ImmutableHashSet<int> AccountNumbers { get; init; } = ImmutableHashSet<int>.Empty;
	}

	private record UnclosedGeneralLedgerEntries : MemoryReadModel {
		public ImmutableHashSet<Guid> NotClosed { get; init; } = ImmutableHashSet<Guid>.Empty;

		public ImmutableDictionary<string, ImmutableHashSet<Guid>> EntriesByPeriod { get; init; } =
			ImmutableDictionary<string, ImmutableHashSet<Guid>>.Empty;
	}
}
