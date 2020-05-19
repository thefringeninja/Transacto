using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Projac;
using Transacto.Messages;

namespace Transacto.Plugins {
	internal class ChartOfAccounts : IPlugin {
		public string Name { get; } = nameof(ChartOfAccounts);

		public void Configure(IEndpointRouteBuilder builder) => builder
			.MapGet(string.Empty, (CancellationToken ct) => {
				var readModel = builder.ServiceProvider.GetRequiredService<InMemoryReadModel>();
				var response =
					!readModel.TryGet<IDictionary<int, (string, bool)>, IDictionary<string, string>>(
						nameof(ChartOfAccounts),
						value => new SortedDictionary<string, string>(
							value.ToDictionary(x => x.Key.ToString(), x => x.Value.Item1)),
						out var chartOfAccounts)
						? (Response)new NotFoundResponse()
						: new HalResponse(new ChartOfAccountRepresentation(), chartOfAccounts);

				return new ValueTask<Response>(response);
			})
			.MapCommands(string.Empty,
				typeof(DefineAccount),
				typeof(RenameAccount),
				typeof(DeactivateAccount),
				typeof(ReactivateAccount));

		public void ConfigureServices(IServiceCollection services) => services
			.AddInMemoryProjection(new AnonymousProjectionBuilder<InMemoryReadModel>()
				.When<AccountDefined>((readModel, e) =>
					readModel.Update(
						nameof(ChartOfAccounts),
						rm => rm.Add(e.AccountNumber, (e.AccountName, true)),
						ReadModel))
				.When<AccountDeactivated>((readModel, e) =>
					readModel.Update(
						nameof(ChartOfAccounts),
						rm => rm[e.AccountNumber] = (rm[e.AccountNumber].accountName, false),
						ReadModel))
				.When<AccountReactivated>((readModel, e) =>
					readModel.Update(
						nameof(ChartOfAccounts),
						rm => rm[e.AccountNumber] = (rm[e.AccountNumber].accountName, true),
						ReadModel))
				.When<AccountRenamed>((readModel, e) =>
					readModel.Update(
						nameof(ChartOfAccounts),
						rm => rm[e.AccountNumber] = (e.NewAccountName, rm[e.AccountNumber].active),
						ReadModel))
				.Build());

		public IEnumerable<Type> MessageTypes => Enumerable.Empty<Type>();

		private static Dictionary<int, (string accountName, bool active)> ReadModel() =>
			new Dictionary<int, (string, bool)>();
	}
}
