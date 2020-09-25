using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Transacto.Framework;
using Transacto.Messages;

namespace Transacto.Plugins.BalanceSheet {
	internal class BalanceSheet : IPlugin {
		public string Name { get; } = nameof(BalanceSheet);

		public void Configure(IEndpointRouteBuilder builder) => builder
			.MapGet("{thru}", context => {
				var readModel = context.RequestServices.GetRequiredService<ReadModel>();

				var thru = DateTimeOffset.Parse(context.GetRouteValue("thru")!.ToString()!);

				return new ValueTask<Response>(new HalResponse(context.Request,
					new BalanceSheetReportRepresentation(), ETag.Create(readModel.Checkpoint), new BalanceSheetReport {
						Thru = thru.UtcDateTime,
						LineItems = readModel.GetLines(thru.UtcDateTime),
						LineItemGroupings = readModel.GetGroupings(thru.UtcDateTime)
					}));
			});

		public void ConfigureServices(IServiceCollection services) => services
			.AddInMemoryProjection<ReadModel>(new BalanceSheetProjection());

		public IEnumerable<Type> MessageTypes => Enumerable.Empty<Type>();

		private class BalanceSheetProjection : InMemoryProjection<ReadModel> {
			public BalanceSheetProjection() {
				When<AccountDefined>((readModel, e) => readModel.Account(e.AccountNumber, e.AccountName));
				When<AccountRenamed>((readModel, e) => readModel.Account(e.AccountNumber, e.NewAccountName));
				When<GeneralLedgerEntryCreated>((readModel, e) =>
					readModel.GeneralLedgerEntryCreated(e.GeneralLedgerEntryId, e.CreatedOn));
				When<DebitApplied>((readModel, e) =>
					readModel.DebitApplied(e.GeneralLedgerEntryId, e.AccountNumber, e.Amount));
				When<CreditApplied>((readModel, e) =>
					readModel.CreditApplied(e.GeneralLedgerEntryId, e.AccountNumber, e.Amount));
				When<GeneralLedgerEntryPosted>((readModel, e) =>
					readModel.GeneralLedgerEntryPosted(e.GeneralLedgerEntryId));
				When<AccountingPeriodClosed>((readModel, e) =>
					readModel.AccountingPeriodClosed(e.GeneralLedgerEntryIds, e.ClosingGeneralLedgerEntryId));
			}
		}

		private class ReadModel : IMemoryReadModel {
			public Optional<Position> Checkpoint { get; set; } = Optional<Position>.Empty;

			private ImmutableDictionary<Guid, Entry> _unpostedEntries;
			private ImmutableDictionary<Guid, Entry> _postedEntries;
			private ImmutableDictionary<int, decimal> _closedBalance;
			private ImmutableDictionary<int, string> _accountNames;

			public ReadModel() {
				_unpostedEntries = ImmutableDictionary<Guid, Entry>.Empty;
				_postedEntries = ImmutableDictionary<Guid, Entry>.Empty;
				_closedBalance = ImmutableDictionary<int, decimal>.Empty;
				_accountNames = ImmutableDictionary<int, string>.Empty;
			}

			public void Account(int accountNumber, string accountName) =>
				Interlocked.Exchange(ref _accountNames, _accountNames.SetItem(accountNumber, accountName));

			public void GeneralLedgerEntryCreated(Guid generalLedgerEntryId, DateTimeOffset createdOn) =>
				Interlocked.Exchange(ref _unpostedEntries, _unpostedEntries.SetItem(generalLedgerEntryId, new Entry {
					CreatedOn = createdOn.UtcDateTime
				}));

			public void DebitApplied(Guid generalLedgerEntryId, int accountNumber, decimal amount) {
				_unpostedEntries[generalLedgerEntryId].Debits[accountNumber] = _unpostedEntries[generalLedgerEntryId]
					.Debits.ContainsKey(accountNumber)
					? _unpostedEntries[generalLedgerEntryId]
						.Debits[accountNumber] + amount
					: amount;
			}

			public void CreditApplied(Guid generalLedgerEntryId, int accountNumber, decimal amount) {
				_unpostedEntries[generalLedgerEntryId].Debits[accountNumber] = _unpostedEntries[generalLedgerEntryId]
					.Credits.ContainsKey(accountNumber)
					? _unpostedEntries[generalLedgerEntryId]
						.Credits[accountNumber] + amount
					: amount;
			}

			public void GeneralLedgerEntryPosted(Guid generalLedgerEntryId) {
				var entry = _unpostedEntries[generalLedgerEntryId];
				Interlocked.Exchange(ref _unpostedEntries, _unpostedEntries.Remove(generalLedgerEntryId));
				Interlocked.Exchange(ref _postedEntries, _postedEntries.SetItem(generalLedgerEntryId, entry));
			}

			public void AccountingPeriodClosed(IEnumerable<Guid> generalLedgerEntryIds,
				Guid closingGeneralLedgerEntryId) {
				foreach (var id in EntryIds()) {
					var entry = _postedEntries[id];
					Interlocked.Exchange(ref _postedEntries, _postedEntries.Remove(id));

					foreach (var (accountNumber, amount) in entry.Debits) {
						Interlocked.Exchange(ref _closedBalance, _closedBalance.SetItem(accountNumber,
							_closedBalance.TryGetValue(accountNumber, out var a)
								? a + amount
								: amount));
					}
					foreach (var (accountNumber, amount) in entry.Credits) {
						Interlocked.Exchange(ref _closedBalance, _closedBalance.SetItem(accountNumber,
							_closedBalance.TryGetValue(accountNumber, out var a)
								? a - amount
								: -amount));
					}

					Interlocked.Exchange(ref _unpostedEntries, _unpostedEntries.Compact());
					Interlocked.Exchange(ref _postedEntries, _postedEntries.Compact());
					Interlocked.Exchange(ref _accountNames, _accountNames.Compact());
					Interlocked.Exchange(ref _closedBalance, _closedBalance.Compact());
				}

				IEnumerable<Guid> EntryIds() {
					foreach (var id in generalLedgerEntryIds) {
						yield return id;
					}

					yield return closingGeneralLedgerEntryId;
				}
			}

			public IList<LineItemGrouping> GetGroupings(DateTime thru) {
				var groupings = _accountNames.ToDictionary(x => x.Key, pair => new LineItemGrouping {
					Name = pair.Value,
					LineItems = {
						new LineItem {
							AccountNumber = pair.Key, Name = pair.Value, Balance = {
								DecimalValue = _closedBalance.TryGetValue(pair.Key, out var amount)
									? amount
									: decimal.Zero
							}
						}
					}
				});
				foreach (var posted in _postedEntries.Values.Where(x => x.CreatedOn <= thru)) {
					foreach (var (accountNumber, amount) in posted.Debits) {
						groupings[accountNumber].LineItems[0].Balance.DecimalValue += amount;
					}

					foreach (var (accountNumber, amount) in posted.Credits) {
						groupings[accountNumber].LineItems[0].Balance.DecimalValue -= amount;
					}
				}

				return groupings.Keys.OrderBy(x => x)
					.Select(x => groupings[x])
					.ToList();
			}

			public IList<LineItem> GetLines(DateTime thru) {
				var groupings = _accountNames.ToDictionary(x => x.Key, pair => new LineItem {
					Name = pair.Value,
					Balance = {
						DecimalValue = decimal.Zero
					},
					AccountNumber = pair.Key
				});
				foreach (var posted in _postedEntries.Values.Where(x => x.CreatedOn <= thru)) {
					foreach (var (accountNumber, amount) in posted.Debits) {
						groupings[accountNumber].Balance.DecimalValue += amount;
					}

					foreach (var (accountNumber, amount) in posted.Credits) {
						groupings[accountNumber].Balance.DecimalValue -= amount;
					}
				}

				return groupings.Keys.OrderBy(x => x)
					.Select(x => groupings[x])
					.ToList();
			}
		}

		private class Entry {
			public DateTime CreatedOn { get; set; }
			public Dictionary<int, decimal> Credits { get; } = new Dictionary<int, decimal>();
			public Dictionary<int, decimal> Debits { get; } = new Dictionary<int, decimal>();
		}
	}
}
