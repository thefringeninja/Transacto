using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Data.SQLite;
using Hallo;
using Microsoft.AspNetCore.Mvc;
using Transacto.Domain;
using Transacto.Framework;
using Transacto.Framework.Http;
using Transacto.Framework.Http.Html;
using Transacto.Framework.Projections;
using Transacto.Infrastructure.EventStore;
using Transacto.Messages;

namespace Transacto.Plugins.GeneralLedger;

internal class GeneralLedger : IPlugin {
	public string Name { get; } = nameof(GeneralLedger);
	public IEnumerable<Type> MessageTypes => [];

	public void Configure(IEndpointRouteBuilder builder) {
		builder
			.MapBusinessTransaction<JournalEntry>("/entries")
			.MapCommands(string.Empty,
			[
				typeof(OpenGeneralLedger),
				typeof(BeginClosingAccountingPeriod)
			]);

		builder.MapGet("", ([FromServices] InMemoryProjectionDatabase database,
			[FromServices] IndexRepresentation representation) => {
			var optionalPeriod = database.Get<CurrentPeriod>();
			var optionalEntries = database.Get<UnclosedGeneralLedgerEntries>();

			var period = optionalPeriod.ToObject<CurrentPeriod>();
			var entries = optionalEntries.HasValue ? optionalEntries.Value.NotClosed : [];

			var checkpoint = period?.Checkpoint.HasValue ?? false
				? period.Checkpoint.Value.ToCheckpoint()
				: Checkpoint.None;
			return Results.Hal(new Index(entries, period?.Period, checkpoint.ToString()), checkpoint, representation);
		});
		builder.MapGet("/period", ([FromServices] InMemoryProjectionDatabase database) => {
			var optional = database.Get<CurrentPeriod>();

			return optional switch {
				{ HasValue: true, Value.Period: { } period } => Results.Hal(
					new Plugins.GeneralLedger.CurrentPeriod(period),
					optional.Value.Checkpoint.ToCheckpoint(), new CurrentPeriodRepresentation()),
				_ => Results.NotFound()
			};
		});
		builder.MapGet("/entries/search", async ([FromQuery] string prefix,
			[FromServices] LedgerEntryIndex.SearchJournalEntriesAsync search,
			[FromServices] LedgerEntryIndex.SearchResultsRepresentation representation,
			CancellationToken ct) => {
			var results = await search(prefix, ct);

			return Results.Hal(results, Checkpoint.None, representation);
		});

		builder.MapGet("/entries/{id:guid}",
			async (Guid id, [FromServices] IGeneralLedgerEntryRepository repository, CancellationToken ct) => {
				using (UnitOfWork.Start()) {
					var optional = await repository.GetOptional(new GeneralLedgerEntryIdentifier(id), ct);
					if (!optional.HasValue) {
						return Results.NotFound();
					}

					return Results.Hal(JournalLedgerEntry.FromGeneralLedgerEntry(optional.Value),
						Checkpoint.None, new JournalEntryRepresentation());
				}
			});
	}

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
		.AddSingleton<IGeneralLedgerEntryRepository, GeneralLedgerEntryEventStoreRepository>()
		.AddInMemoryProjection<DeactivatedAccounts>((readModel, e) => readModel with {
			AccountNumbers = e switch {
				AccountDeactivated d => readModel.AccountNumbers.Add(d.AccountNumber),
				AccountReactivated r => readModel.AccountNumbers.Remove(r.AccountNumber),
				_ => readModel.AccountNumbers
			}
		})
		.AddSQLiteProjection<LedgerEntryIndex.LedgerEntryIndexProjection>()
		.AddSingleton<LedgerEntryIndex.SearchJournalEntriesAsync>(provider => {
			var connectionFactory =
				provider.GetRequiredKeyedService<Func<SQLiteConnection>>(
					typeof(LedgerEntryIndex.LedgerEntryIndexProjection));

			return async (prefix, ct) => {
				var results = new List<LedgerEntryIndex.SearchResult>(13);
				await using var connection = connectionFactory();
				await connection.OpenAsync(ct);

				await using var command = new SQLiteCommand(connection) {
					CommandText = """
					              SELECT reference_number, general_ledger_entry_id, created_on 
					              FROM ledger_entry_index 
					              WHERE reference_number LIKE CONCAT(@prefix, '%')
					              LIMIT 25
					              """
				};
				command.Parameters.AddWithValue("@prefix", prefix);
				await using var reader = await command.ExecuteReaderAsync(ct);

				while (await reader.ReadAsync(ct)) {
					results.Add(new LedgerEntryIndex.SearchResult(
						reader.GetString(0),
						reader.GetGuid(1),
						reader.GetDateTime(2)));
				}

				return new LedgerEntryIndex.SearchResults([..results]);
			};
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
		.AddInMemoryProjection<CurrentPeriod>((readModel, e) => readModel with {
			Period = e switch {
				GeneralLedgerOpened o => o.OpenedOn[..6],
				AccountingPeriodClosed c => AccountingPeriod.Parse(c.Period).Next().ToString(),
				_ => readModel.Period
			}
		})
		.AddSingleton<IndexRepresentation>()
		.AddProcessManager<AccountingPeriodClosingModule>("accountingPeriodClosingCheckpoint");

	private record DeactivatedAccounts : MemoryReadModel {
		public ImmutableHashSet<int> AccountNumbers { get; init; } = ImmutableHashSet<int>.Empty;
	}

	private record UnclosedGeneralLedgerEntries : MemoryReadModel {
		public ImmutableHashSet<Guid> NotClosed { get; init; } = ImmutableHashSet<Guid>.Empty;

		public ImmutableDictionary<string, ImmutableHashSet<Guid>> EntriesByPeriod { get; init; } =
			ImmutableDictionary<string, ImmutableHashSet<Guid>>.Empty;
	}

	private record CurrentPeriod : MemoryReadModel {
		public string? Period { get; init; }
	}

	private record Index(ImmutableHashSet<Guid> UnclosedGeneralLedgerEntryIds, string? Period, string Checkpoint);

	private class IndexRepresentation(SiteRoot siteRoot) : Hal<Index>, IHalState<Index>, IHalLinks<Index> {
		public object StateFor(Index resource) => resource;

		public IEnumerable<Link> LinksFor(Index resource) {
			yield return new Link("self", siteRoot.Uri.AbsolutePath);
		}
	}
}
