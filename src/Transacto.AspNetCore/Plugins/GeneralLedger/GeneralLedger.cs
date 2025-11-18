using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Data.SQLite;
using Hallo;
using Microsoft.AspNetCore.Mvc;
using Transacto.Domain;
using Transacto.Framework;
using Transacto.Framework.Http;
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
				typeof(OpenGeneralLedger),
				typeof(BeginClosingAccountingPeriod));

		builder.MapGet("/entries/search", async ([FromQuery] string prefix,
			[FromServices] LedgerEntryIndex.SearchJournalEntriesAsync search, CancellationToken ct) => {
			var results = await search(prefix, ct);

			return Results.Extensions.Hal(results, Checkpoint.None, new LedgerEntryIndex.SearchResultRepresentation());
		});
		
		builder.MapGet("/entries/{id:guid}",
			async (Guid id, [FromServices] IGeneralLedgerEntryRepository repository, CancellationToken ct) => {
				using (UnitOfWork.Start()) {
					var optional = await repository.GetOptional(new GeneralLedgerEntryIdentifier(id), ct);
					if (!optional.HasValue) {
						return Results.NotFound();
					}

					return Results.Extensions.Hal(JournalLedgerEntry.FromGeneralLedgerEntry(optional.Value),
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
				connection.Open();

				await using var command = new SQLiteCommand(connection) {
					CommandText = """
					              SELECT reference_number, general_ledger_entry_id, created_on 
					              FROM ledger_entry_index 
					              WHERE reference_number LIKE CONCAT(@prefix, '%')
					              LIMIT 25
					              """,
					Parameters = {
						new SQLiteParameter(prefix) {
							ParameterName = "@prefix"
						}
					}
				};
				await using var reader = command.ExecuteReader();

				while (reader.Read()) {
					results.Add(new LedgerEntryIndex.SearchResult(reader.GetString(0), reader.GetGuid(1),
						reader.GetDateTime(2)));
				}

				return [..results];
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
		.AddProcessManager<AccountingPeriodClosingModule>("accountingPeriodClosingCheckpoint");

	private record DeactivatedAccounts : MemoryReadModel {
		public ImmutableHashSet<int> AccountNumbers { get; init; } = ImmutableHashSet<int>.Empty;
	}

	private record UnclosedGeneralLedgerEntries : MemoryReadModel {
		public ImmutableHashSet<Guid> NotClosed { get; init; } = ImmutableHashSet<Guid>.Empty;

		public ImmutableDictionary<string, ImmutableHashSet<Guid>> EntriesByPeriod { get; init; } =
			ImmutableDictionary<string, ImmutableHashSet<Guid>>.Empty;
	}


	public class JournalEntryRepresentation : Hal<JournalLedgerEntry>, IHalState<JournalLedgerEntry> {
		public object StateFor(JournalLedgerEntry resource) => resource;
	}

	public record JournalLedgerEntry(
		string ReferenceNumber,
		IBusinessTransaction? BusinessTransaction,
		ImmutableArray<JournalLedgerEntry.Item> Items) {
		public static JournalLedgerEntry FromGeneralLedgerEntry(GeneralLedgerEntry entry) {
			var items = entry.Debits.Select(d => new Item(d.AccountNumber.ToInt32(), d.Amount.ToDecimal(), Type.Debit))
				.Concat(
					entry.Credits.Select(c => new Item(c.AccountNumber.ToInt32(), c.Amount.ToDecimal(), Type.Credit)))
				.OrderBy(i => i.AccountNumber)
				.ToImmutableArray();
			return new JournalLedgerEntry(entry.ReferenceNumber.ToString(), entry.BusinessTransaction, items);
		}

		public enum Type { Debit, Credit };

		public record Item(int AccountNumber, decimal Amount, Type Type);
	}
}
