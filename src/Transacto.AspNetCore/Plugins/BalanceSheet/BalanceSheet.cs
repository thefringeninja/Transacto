using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventStore.Client;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Transacto.Framework;
using Transacto.Messages;

namespace Transacto.Plugins.BalanceSheet {
	internal class
		BalanceSheet : IPlugin {
		public string Name { get; } = nameof(BalanceSheet);

		public void Configure(IEndpointRouteBuilder builder) => builder
			.MapGet("{thru}", context => {
				var readModel = context.RequestServices.GetRequiredService<InMemoryReadModel>();

				var thru = DateTimeOffset.Parse(context.GetRouteValue("thru")!.ToString()!);

				if (!readModel.TryGetValue<ReadModel>(nameof(BalanceSheet), out var entry)) {
					return new ValueTask<Response>(new NotFoundResponse());
				}

				using (entry!.Read()) {
					return new ValueTask<Response>(new HalResponse(context.Request,
						new BalanceSheetReportRepresentation(), ETag.Create(entry.Item.Checkpoint), new BalanceSheetReport {
							Thru = thru.UtcDateTime,
							LineItems = entry.Item.GetLines(thru.UtcDateTime),
							LineItemGroupings = entry.Item.GetGroupings(thru.UtcDateTime)
						}));
				}
			});

		public void ConfigureServices(IServiceCollection services) => services
			.AddInMemoryProjection(new InMemoryProjectionBuilder()
				.When<AccountDefined>((readModel, e) =>
					readModel.AddOrUpdate(
						nameof(BalanceSheet),
						ReadModel.Factory, _ => {
							_.AccountNames[e.Message.AccountNumber] = e.Message.AccountName;
							_.Checkpoint = e.Position;
						}))
				.When<AccountRenamed>((readModel, e) =>
					readModel.AddOrUpdate(
						nameof(BalanceSheet),
						ReadModel.Factory, _ => {
							_.AccountNames[e.Message.AccountNumber] = e.Message.NewAccountName;
							_.Checkpoint = e.Position;
						}))
				.When<GeneralLedgerEntryCreated>((readModel, e) =>
					readModel.AddOrUpdate(
						nameof(BalanceSheet), ReadModel.Factory, _ => {
							_.UnpostedEntries.TryAdd(e.Message.GeneralLedgerEntryId,
								new Entry {
									CreatedOn = e.Message.CreatedOn.UtcDateTime
								});
							_.Checkpoint = e.Position;
						}))
				.When<DebitApplied>((readModel, e) =>
					readModel.AddOrUpdate(
						nameof(BalanceSheet),
						ReadModel.Factory, _ => {
							_.UnpostedEntries[e.Message.GeneralLedgerEntryId].Debits[e.Message.AccountNumber] =
								_.UnpostedEntries[e.Message.GeneralLedgerEntryId].Debits
									.ContainsKey(e.Message.AccountNumber)
									? _.UnpostedEntries[e.Message.GeneralLedgerEntryId]
										  .Debits[e.Message.AccountNumber] +
									  e.Message.Amount
									: e.Message.Amount;
							_.Checkpoint = e.Position;
						}))
				.When<CreditApplied>((readModel, e) =>
					readModel.AddOrUpdate(
						nameof(BalanceSheet),
						ReadModel.Factory, _ => {
							_.UnpostedEntries[e.Message.GeneralLedgerEntryId].Credits[e.Message.AccountNumber] =
								_.UnpostedEntries[e.Message.GeneralLedgerEntryId].Credits
									.ContainsKey(e.Message.AccountNumber)
									? _.UnpostedEntries[e.Message.GeneralLedgerEntryId]
										  .Credits[e.Message.AccountNumber] +
									  e.Message.Amount
									: e.Message.Amount;
							_.Checkpoint = e.Position;
						}))
				.When<GeneralLedgerEntryPosted>((readModel, e) =>
					readModel.AddOrUpdate(
						nameof(BalanceSheet), ReadModel.Factory, _ => {
							var entry = _.UnpostedEntries[e.Message.GeneralLedgerEntryId];
							_.UnpostedEntries.Remove(e.Message.GeneralLedgerEntryId);
							_.PostedEntries[e.Message.GeneralLedgerEntryId] = entry;
							_.Checkpoint = e.Position;
						}))
				.When<AccountingPeriodClosed>((readModel, e) => {
					readModel.AddOrUpdate(
						nameof(BalanceSheet), ReadModel.Factory, _ => {
							foreach (var id in e.Message.GeneralLedgerEntryIds.Concat(new[]
								{e.Message.ClosingGeneralLedgerEntryId})) {
								var entry = _.PostedEntries[id];
								_.PostedEntries.Remove(id);

								foreach (var (accountNumber, amount) in entry.Debits) {
									_.ClosedBalance[accountNumber] =
										_.ClosedBalance.TryGetValue(accountNumber, out var a)
											? a + amount
											: amount;
								}

								foreach (var (accountNumber, amount) in entry.Credits) {
									_.ClosedBalance[accountNumber] =
										_.ClosedBalance.TryGetValue(accountNumber, out var a)
											? a - amount
											: -amount;
								}
							}
						});
				})
				.Build());

		public IEnumerable<Type> MessageTypes => Enumerable.Empty<Type>();

		private class ReadModel {
			public static ReadModel Factory() => new ReadModel();

			private ReadModel() {
			}

			public Optional<Position> Checkpoint { get; set; } = Optional<Position>.Empty;

			public Dictionary<Guid, Entry> UnpostedEntries { get; } = new Dictionary<Guid, Entry>();
			public Dictionary<Guid, Entry> PostedEntries { get; } = new Dictionary<Guid, Entry>();
			public Dictionary<int, decimal> ClosedBalance { get; } = new Dictionary<int, decimal>();
			public Dictionary<int, string> AccountNames { get; } = new Dictionary<int, string>();

			public IList<LineItemGrouping> GetGroupings(DateTime thru) {
				var groupings = AccountNames.ToDictionary(x => x.Key, pair => new LineItemGrouping {
					Name = pair.Value,
					LineItems = {
						new LineItem {
							AccountNumber = pair.Key, Name = pair.Value, Balance = {
								DecimalValue = ClosedBalance.TryGetValue(pair.Key, out var amount)
									? amount
									: decimal.Zero
							}
						}
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
