using System.Collections.Immutable;
using Hallo;
using Microsoft.AspNetCore.Mvc;
using NodaTime;
using NodaTime.Text;
using Transacto.Domain;
using Transacto.Framework;
using Transacto.Framework.Http;
using Transacto.Framework.Projections;
using Transacto.Infrastructure.EventStore;
using Transacto.Messages;

namespace Transacto.Plugins.BalanceSheet;

internal class BalanceSheet : IPlugin {
	public string Name { get; } = nameof(BalanceSheet);

	public void Configure(IEndpointRouteBuilder builder) => builder
		.MapGet("{thru}", (LocalDateTimeData thru, [FromServices] InMemoryProjectionDatabase database) =>
			database.Get<ReadModel>() switch {
				{ HasValue: true } optional => Results.Extensions.Hal(new BalanceSheetReport {
					Thru = (thru.Value ?? LocalDateTime.MaxIsoValue).ToDateTimeUnspecified(),
					LineItems = optional.Value.GetLineItems(thru.Value ?? LocalDateTime.MaxIsoValue),
					LineItemGroupings = ImmutableArray<LineItemGrouping>.Empty
				}, optional.Value.Checkpoint.ToCheckpoint(), new BalanceSheetReportRepresentation()),
				_ => Results.Extensions.Hal(ReadModel.None, Checkpoint.None, new BalanceSheetReportRepresentation())
			});

	private record struct LocalDateTimeData {
		// ReSharper disable once UnusedMember.Local
		public static bool TryParse(string? value, IFormatProvider? provider, out LocalDateTimeData parameter) {
			parameter = value switch {
				null => None,
				_ => LocalDateTimePattern.ExtendedIso.Parse(value) switch {
					{ Success: true } result => new() { Value = result.Value },
					_ => None
				}
			};

			return parameter != None;
		}

		private static readonly LocalDateTimeData None = new() { Value = null };

		public required LocalDateTime? Value { get; init; }
	}

	private record Entry(LocalDateTime CreatedOn) {
		public ImmutableDictionary<int, decimal> Credits { get; init; } = ImmutableDictionary<int, decimal>.Empty;
		public ImmutableDictionary<int, decimal> Debits { get; init; } = ImmutableDictionary<int, decimal>.Empty;
	}

	private record ReadModel : MemoryReadModel {
		public static readonly ReadModel None = new();

		public ImmutableDictionary<Guid, Entry> UnpostedEntries { get; init; } =
			ImmutableDictionary<Guid, Entry>.Empty;

		public ImmutableDictionary<Guid, Entry> PostedEntries { get; init; } =
			ImmutableDictionary<Guid, Entry>.Empty;

		public ImmutableDictionary<int, decimal> ClosedBalance { get; init; } =
			ImmutableDictionary<int, decimal>.Empty;

		public ImmutableDictionary<int, string> AccountNames { get; init; } =
			ImmutableDictionary<int, string>.Empty;

		public ImmutableSortedSet<Entry> PostedEntriesByDate { get; init; } = ImmutableSortedSet<Entry>.Empty;

		public ImmutableArray<LineItem> GetLineItems(LocalDateTime thru) => PostedEntries.Values
			.Where(entry => entry.CreatedOn < thru)
			.Aggregate(ClosedBalance, (closedBalance, entry) => entry.Debits.Concat(entry.Credits
					.Select(x => new KeyValuePair<int, decimal>(x.Key, -x.Value)))
				.Aggregate(closedBalance, (cb, pair) => cb.SetItem(pair.Key,
					cb.TryGetValue(pair.Key, out var balance)
						? balance + pair.Value
						: pair.Value)))
			.Select(pair => new LineItem {
				AccountNumber = pair.Key,
				Balance = Decimal.Create(pair.Value),
				Name = AccountNames[pair.Key]
			})
			.ToImmutableArray();

		public ReadModel Compact() => this with {
			UnpostedEntries = UnpostedEntries.Compact(),
			PostedEntries = PostedEntries.Compact(),
			ClosedBalance = ClosedBalance.Compact(),
			AccountNames = AccountNames.Compact(),
			PostedEntriesByDate = PostedEntriesByDate.Compact()
		};
	}

	private class BalanceSheetReportRepresentation : Hal<BalanceSheetReport>, IHalLinks<BalanceSheetReport>,
		IHalState<BalanceSheetReport> {
		public IEnumerable<Link> LinksFor(BalanceSheetReport resource) {
			yield break;
		}

		public object StateFor(BalanceSheetReport resource) => resource;
	}

	public void ConfigureServices(IServiceCollection services) => services
		.AddInMemoryProjection<ReadModel>((readModel, e) => e switch {
			AccountDefined d => readModel with {
				AccountNames = readModel.AccountNames.SetItem(d.AccountNumber, d.AccountName)
			},
			AccountRenamed r => readModel with {
				AccountNames = readModel.AccountNames.SetItem(r.AccountNumber, r.NewAccountName)
			},
			GeneralLedgerEntryCreated c => readModel with {
				UnpostedEntries = readModel.UnpostedEntries.SetItem(c.GeneralLedgerEntryId,
					new Entry(Time.Parse.LocalDateTime(c.CreatedOn)))
			},
			DebitApplied a => readModel with {
				UnpostedEntries = readModel.UnpostedEntries.SetItem(a.GeneralLedgerEntryId,
					readModel.UnpostedEntries[a.GeneralLedgerEntryId] with {
						Debits = readModel.UnpostedEntries[a.GeneralLedgerEntryId].Debits
							.SetItem(a.AccountNumber, readModel.UnpostedEntries[a.GeneralLedgerEntryId].Debits
								.TryGetValue(a.AccountNumber, out var amount)
								? amount + a.Amount
								: a.Amount)
					})
			},
			CreditApplied a => readModel with {
				UnpostedEntries = readModel.UnpostedEntries.SetItem(a.GeneralLedgerEntryId,
					readModel.UnpostedEntries[a.GeneralLedgerEntryId] with {
						Credits = readModel.UnpostedEntries[a.GeneralLedgerEntryId].Credits
							.SetItem(a.AccountNumber, readModel.UnpostedEntries[a.GeneralLedgerEntryId].Credits
								.TryGetValue(a.AccountNumber, out var amount)
								? amount + a.Amount
								: a.Amount)
					})
			},
			GeneralLedgerEntryPosted p => readModel with {
				PostedEntries = readModel.PostedEntries.Add(p.GeneralLedgerEntryId,
					readModel.UnpostedEntries.TryGetValue(p.GeneralLedgerEntryId, out var entry)
						? entry
						: throw new InvalidOperationException()),
				UnpostedEntries = readModel.UnpostedEntries.Remove(p.GeneralLedgerEntryId),
				PostedEntriesByDate =
				readModel.PostedEntriesByDate.Add(readModel.UnpostedEntries[p.GeneralLedgerEntryId])
			},
			AccountingPeriodClosed c => (readModel with {
				PostedEntries = c.GeneralLedgerEntryIds.Add(c.ClosingGeneralLedgerEntryId)
					.Aggregate(readModel.PostedEntries, (postedEntries, id) => postedEntries.Remove(id)),
				ClosedBalance = readModel.PostedEntries.Keys.Aggregate(readModel.ClosedBalance,
					(closedBalance, id) => readModel.PostedEntries[id].Debits.Concat(
							readModel.PostedEntries[id].Credits
								.Select(x => new KeyValuePair<int, decimal>(x.Key, -x.Value)))
						.Aggregate(closedBalance,
							(cb, pair) => cb.SetItem(pair.Key,
								cb.TryGetValue(pair.Key, out var balance)
									? balance + pair.Value
									: pair.Value))),
				PostedEntriesByDate = c.GeneralLedgerEntryIds.Add(c.ClosingGeneralLedgerEntryId)
					.Aggregate(readModel.PostedEntriesByDate,
						(postedEntries, id) => postedEntries.Remove(readModel.PostedEntries[id]))
			}).Compact(),
			_ => readModel
		});

	public IEnumerable<Type> MessageTypes => Enumerable.Empty<Type>();
}
