using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using EventStore.Client;
using Hallo;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Transacto.Framework;
using Transacto.Framework.Http;
using Transacto.Framework.Projections;
using Transacto.Messages;

namespace Transacto.Plugins.ChartOfAccounts {
	internal class ChartOfAccounts : IPlugin {
		public string Name { get; } = nameof(ChartOfAccounts);

		public void Configure(IEndpointRouteBuilder builder) => builder
			.MapGet(string.Empty, context => {
				var readModel = context.RequestServices.GetRequiredService<InMemoryProjectionDatabase>()
					.Get<ReadModel>();

				return new(HalResponse.Create(context.Request, new ChartOfAccountsRepresentation(),
					readModel.HasValue ? readModel.Value.Checkpoint : Optional<Position>.Empty,
					readModel));
			})
			.MapCommands(string.Empty,
				typeof(DefineAccount),
				typeof(RenameAccount),
				typeof(DeactivateAccount),
				typeof(ReactivateAccount));

		public void ConfigureServices(IServiceCollection services) => services
			.AddCommandHandlerModule<ChartOfAccountsModule>()
			.AddInMemoryProjection(new InMemoryProjectionBuilder<ReadModel>()
				.When((readModel, e) => readModel with {
					ChartOfAccounts = e switch {
						AccountDefined d => readModel.ChartOfAccounts.SetItem(d.AccountNumber, (d.AccountName, true)),
						AccountRenamed r => readModel.ChartOfAccounts.SetItem(r.AccountNumber,
							(r.NewAccountName, readModel.ChartOfAccounts[r.AccountNumber].active)),
						AccountDeactivated d => readModel.ChartOfAccounts.SetItem(d.AccountNumber,
							(readModel.ChartOfAccounts[d.AccountNumber].accountName, false)),
						AccountReactivated r => readModel.ChartOfAccounts.SetItem(r.AccountNumber,
							(readModel.ChartOfAccounts[r.AccountNumber].accountName, true)),
						_ => readModel.ChartOfAccounts
					}
				})
				.Build());

		public IEnumerable<Type> MessageTypes => Enumerable.Empty<Type>();

		private class ChartOfAccountsRepresentation : Hal<ReadModel>, IHalLinks<ReadModel>, IHalState<ReadModel> {
			public IEnumerable<Link> LinksFor(ReadModel resource) {
				foreach (var (accountNumber, (accountName, _)) in resource.ChartOfAccounts) {
					yield return new Link("self", $"chart-of-accounts/{accountNumber}", title: accountName);
				}
			}

			public object StateFor(ReadModel resource) => new SortedDictionary<string, string>(
				resource.ChartOfAccounts.ToDictionary(x => x.Key.ToString(), x => x.Value.accountName));
		}

		private record ReadModel : MemoryReadModel {
			public ImmutableSortedDictionary<int, (string accountName, bool active)> ChartOfAccounts { get; init; } =
				ImmutableSortedDictionary<int, (string, bool)>.Empty;
		}
	}
}
