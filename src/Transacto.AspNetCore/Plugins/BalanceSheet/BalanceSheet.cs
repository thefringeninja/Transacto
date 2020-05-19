using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hallo;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Projac;
using Transacto.Messages;

namespace Transacto.Plugins.BalanceSheet {
	internal class BalanceSheet : IPlugin {
		public string Name { get; } = nameof(BalanceSheet);

		public void Configure(IEndpointRouteBuilder builder) => builder
			.MapGet("{thru}", context => {
				var readModel = context.RequestServices.GetRequiredService<InMemoryReadModel>();

				var thru = DateTimeOffset.Parse(context.GetRouteValue("thru")!.ToString()!);

				return !readModel.TryGet<ReadModel, BalanceSheetReport>(nameof(BalanceSheet),
					balanceSheet => new BalanceSheetReport {
						Thru = thru.UtcDateTime,
						LineItems = balanceSheet.GetLines(thru.UtcDateTime),
						LineItemGroupings = balanceSheet.GetGroupings(thru.UtcDateTime)
					}, out var report)
					? new ValueTask<Response>(new NotFoundResponse())
					: new ValueTask<Response>(new HalResponse(new BalanceSheetReportRepresentation(), report));
			});

		public void ConfigureServices(IServiceCollection services)
			=> services.AddInMemoryProjection(new AnonymousProjectionBuilder<InMemoryReadModel>()
				.When<AccountDefined>((readModel, e) =>
					readModel.Update(nameof(BalanceSheet), _ => _.AccountNames[e.AccountNumber] = e.AccountName,
						ReadModel.Factory))
				.When<AccountRenamed>((readModel, e) =>
					readModel.Update(nameof(BalanceSheet), _ => _.AccountNames[e.AccountNumber] = e.NewAccountName,
						ReadModel.Factory))
				.When<GeneralLedgerEntryCreated>((readModel, e) =>
					readModel.Update(nameof(BalanceSheet), _ => _.UnpostedEntries.TryAdd(e.GeneralLedgerEntryId,
						new Entry {
							CreatedOn = e.CreatedOn.UtcDateTime
						}), ReadModel.Factory))
				.When<DebitApplied>((readModel, e) =>
					readModel.Update(nameof(BalanceSheet),
						_ => _.UnpostedEntries[e.GeneralLedgerEntryId].Debits[e.AccountNumber] =
							_.UnpostedEntries[e.GeneralLedgerEntryId].Debits.ContainsKey(e.AccountNumber)
								? _.UnpostedEntries[e.GeneralLedgerEntryId].Debits[e.AccountNumber] + e.Amount
								: e.Amount,
						ReadModel.Factory))
				.When<CreditApplied>((readModel, e) =>
					readModel.Update(nameof(BalanceSheet),
						_ => _.UnpostedEntries[e.GeneralLedgerEntryId].Credits[e.AccountNumber] =
							_.UnpostedEntries[e.GeneralLedgerEntryId].Credits.ContainsKey(e.AccountNumber)
								? _.UnpostedEntries[e.GeneralLedgerEntryId].Credits[e.AccountNumber] + e.Amount
								: e.Amount,
						ReadModel.Factory))
				.When<GeneralLedgerEntryPosted>((readModel, e) =>
					readModel.Update(nameof(BalanceSheet),
						_ => {
							var entry = _.UnpostedEntries[e.GeneralLedgerEntryId];
							_.UnpostedEntries.Remove(e.GeneralLedgerEntryId);
							_.PostedEntries[e.GeneralLedgerEntryId] = entry;
						}, ReadModel.Factory))
				.When<AccountingPeriodClosed>((readModel, e) => {
					readModel.Update(nameof(BalanceSheet), _ => {
						foreach (var id in e.GeneralLedgerEntryIds.Concat(new[] {e.ClosingGeneralLedgerEntryId})) {
							var entry = _.PostedEntries[id];
							_.PostedEntries.Remove(id);

							foreach (var (accountNumber, amount) in entry.Debits) {
								_.ClosedBalance[accountNumber] = _.ClosedBalance.TryGetValue(accountNumber, out var a)
									? a + amount
									: amount;
							}

							foreach (var (accountNumber, amount) in entry.Credits) {
								_.ClosedBalance[accountNumber] = _.ClosedBalance.TryGetValue(accountNumber, out var a)
									? a - amount
									: -amount;
							}
						}
					}, ReadModel.Factory);
				})
				.Build());

		public IEnumerable<Type> MessageTypes => Enumerable.Empty<Type>();

		private class BalanceSheetReportRepresentation : Hal<BalanceSheetReport>, IHalLinks<BalanceSheetReport>,
			IHalState<BalanceSheetReport> {
			public IEnumerable<Link> LinksFor(BalanceSheetReport resource) {
				yield break;
			}

			public object StateFor(BalanceSheetReport resource) => resource;
		}

		private class ReadModel {
			public static ReadModel Factory() => new ReadModel();

			private ReadModel() {
			}

			public Dictionary<Guid, Entry> UnpostedEntries { get; } = new Dictionary<Guid, Entry>();
			public Dictionary<Guid, Entry> PostedEntries { get; } = new Dictionary<Guid, Entry>();
			public Dictionary<int, decimal> ClosedBalance { get; } = new Dictionary<int, decimal>();
			public Dictionary<int, string> AccountNames { get; } = new Dictionary<int, string>();

			public IList<LineItemGrouping> GetGroupings(DateTime thru) {
				var groupings = AccountNames.ToDictionary(x => x.Key, pair => new LineItemGrouping {
					Name = pair.Value,
					LineItems = {
						new LineItem {AccountNumber = pair.Key, Name = pair.Value, Balance = {
							DecimalValue = ClosedBalance.TryGetValue(pair.Key, out var amount)
								? amount
								: decimal.Zero
						}}
					}
				});
				foreach (var posted in PostedEntries.Values.Where(x => x.CreatedOn <= thru)) {
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
				var groupings = AccountNames.ToDictionary(x => x.Key, pair => new LineItem {
					Name = pair.Value,
					Balance = {
						DecimalValue = decimal.Zero
					},
					AccountNumber = pair.Key
				});
				foreach (var posted in PostedEntries.Values.Where(x => x.CreatedOn <= thru)) {
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
