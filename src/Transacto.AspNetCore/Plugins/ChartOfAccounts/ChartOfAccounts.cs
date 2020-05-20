using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using EventStore.Client;
using Hallo;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Transacto.Framework;
using Transacto.Messages;

namespace Transacto.Plugins.ChartOfAccounts {
	internal class ChartOfAccounts : IPlugin {
		public string Name { get; } = nameof(ChartOfAccounts);

		public void Configure(IEndpointRouteBuilder builder) => builder
			.MapGet(string.Empty, context => {
				var readModel = context.RequestServices.GetRequiredService<InMemoryReadModel>();

				var hasValue = readModel.TryGetValue<ReadModel>(
					nameof(ChartOfAccounts),
					out var entry);

				var statusCode = hasValue
					? HttpStatusCode.OK
					: HttpStatusCode.NotFound;
				var response = new HalResponse(context.Request, new ChartOfAccountsRepresentation(),
					ETag.Create(entry?.Item.Checkpoint ?? Optional<Position>.Empty),
					hasValue ? new Optional<object>(entry?.Item!) : Optional<object>.Empty);
				if (response.StatusCode != HttpStatusCode.NotAcceptable) {
					response.StatusCode = statusCode;
				}

				return new ValueTask<Response>(response);
			})
			.MapCommands(string.Empty,
				typeof(DefineAccount),
				typeof(RenameAccount),
				typeof(DeactivateAccount),
				typeof(ReactivateAccount));

		public void ConfigureServices(IServiceCollection services) => services
			.AddInMemoryProjection(new InMemoryProjectionBuilder()
				.When<AccountDefined>((readModel, e) =>
					readModel.AddOrUpdate(
						nameof(ChartOfAccounts),
						ReadModel.Factory, rm => {
							rm.Checkpoint = e.Position;
							rm.List.TryAdd(e.Message.AccountNumber, (e.Message.AccountName, true));
						}))
				.When<AccountDeactivated>((readModel, e) =>
					readModel.AddOrUpdate(
						nameof(ChartOfAccounts),
						ReadModel.Factory, rm => {
							rm.Checkpoint = e.Position;
							rm.List[e.Message.AccountNumber] = (rm.List[e.Message.AccountNumber].accountName, false);
						}))
				.When<AccountReactivated>((readModel, e) =>
					readModel.AddOrUpdate(
						nameof(ChartOfAccounts),
						ReadModel.Factory, rm => {
							rm.Checkpoint = e.Position;
							rm.List[e.Message.AccountNumber] = (rm.List[e.Message.AccountNumber].accountName, true);
						}))
				.When<AccountRenamed>((readModel, e) =>
					readModel.AddOrUpdate(
						nameof(ChartOfAccounts),
						ReadModel.Factory, rm => {
							rm.Checkpoint = e.Position;
							rm.List[e.Message.AccountNumber] =
								(e.Message.NewAccountName, rm.List[e.Message.AccountNumber].active);
						}))
				.Build());

		public IEnumerable<Type> MessageTypes => Enumerable.Empty<Type>();

		private class ChartOfAccountsRepresentation : Hal<ReadModel>,
			IHalLinks<ReadModel>,
			IHalState<ReadModel> {
			public IEnumerable<Link> LinksFor(ReadModel resource) {
				yield break;
			}

			public object StateFor(ReadModel resource) =>
				new SortedDictionary<string, string>(resource.List.ToDictionary(x => x.Key.ToString(),
					x => x.Value.accountName));
		}

		private class ReadModel {
			public static ReadModel Factory() => new ReadModel();

			public ConcurrentDictionary<int, (string accountName, bool active)> List { get; } =
				new ConcurrentDictionary<int, (string accountName, bool active)>();

			public Optional<Position> Checkpoint { get; set; }
		}
	}
}
