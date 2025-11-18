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
		private static readonly LocalDateTimePattern[] Patterns = [
			LocalDateTimePattern.GeneralIso,
			LocalDateTimePattern.CreateWithInvariantCulture("yyyy-MM-dd")
		];

		// ReSharper disable once UnusedMember.Local
		public static bool TryParse(string? value, IFormatProvider? provider, out LocalDateTimeData parameter) {
			parameter = None;
			if (value is null) {
				return false;
			}

			for (var i = 0; i < Patterns.Length; i++) {
				var parseResult = Patterns[i].Parse(value);
				if (!parseResult.Success) continue;
				parameter = new() { Value = parseResult.Value };
				return true;
			}

			return false;
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

		public ImmutableHashSet<Entry> PostedEntriesByDate { get; init; } = ImmutableHashSet<Entry>.Empty;

		public ImmutableArray<LineItem> GetLineItems(LocalDateTime thru) => [
			..PostedEntries.Values
				.Where(entry => entry.CreatedOn < thru)
				.Aggregate(ClosedBalance, (closedBalance, entry) => entry.Debits.Concat(entry.Credits)
					.Aggregate(closedBalance, (cb, pair) => cb.SetItem(pair.Key,
						cb.TryGetValue(pair.Key, out var balance)
							? balance + pair.Value
							: pair.Value)))
				.Select(pair => new LineItem {
					AccountNumber = pair.Key,
					Balance = Decimal.Create(pair.Value),
					Name = AccountNames[pair.Key]
				})
		];

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
		.AddInMemoryProjection<ReadModel>((readModel, e) => {
			return e switch {
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
				DebitApplied a => DebitApplied(a),
				CreditApplied a => CreditApplied(a),
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
					ClosedBalance = c.Balance.Where(item =>
							Account.For(new AccountNumber(item.AccountNumber)) is EquityAccount or LiabilityAccount
								or AssetAccount)
						.ToImmutableDictionary(item => item.AccountNumber, item => item.Amount),
					PostedEntriesByDate = c.GeneralLedgerEntryIds.Add(c.ClosingGeneralLedgerEntryId)
						.Aggregate(readModel.PostedEntriesByDate,
							(postedEntries, id) => postedEntries.Remove(readModel.PostedEntries[id]))
				}).Compact(),
				_ => readModel
			};

			ReadModel DebitApplied(DebitApplied e) {
				var account = Account.For(new AccountNumber(e.AccountNumber));
				if (account is not (EquityAccount or AssetAccount or LiabilityAccount))
					return readModel;

				var a = account.Debit(new Money(e.Amount)).Balance.ToDecimal();

				var unpostedEntry = readModel.UnpostedEntries[e.GeneralLedgerEntryId];
				var debits = unpostedEntry.Debits;
				return readModel with {
					UnpostedEntries = readModel.UnpostedEntries.SetItem(e.GeneralLedgerEntryId,
						unpostedEntry with {
							Debits = debits.SetItem(e.AccountNumber, debits.TryGetValue(e.AccountNumber, out var amount)
								? amount + a
								: a)
						})
				};
			}

			ReadModel CreditApplied(CreditApplied e) {
				var account = Account.For(new AccountNumber(e.AccountNumber));
				if (account is not (EquityAccount or AssetAccount or LiabilityAccount))
					return readModel;

				var a = account.Credit(new Money(e.Amount)).Balance.ToDecimal();

				var unpostedEntry = readModel.UnpostedEntries[e.GeneralLedgerEntryId];

				var credits = unpostedEntry.Credits;
				return readModel with {
					UnpostedEntries = readModel.UnpostedEntries.SetItem(e.GeneralLedgerEntryId,
						unpostedEntry with {
							Credits = credits.SetItem(e.AccountNumber,
								credits.TryGetValue(e.AccountNumber, out var amount)
									? amount + a
									: a)
						})
				};
			}
		});

	public IEnumerable<Type> MessageTypes => [];
}
