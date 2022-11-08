using System.Collections.Immutable;
using Hallo;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Transacto.Framework;
using Transacto.Framework.Http;
using Transacto.Framework.Projections;
using Transacto.Messages;

namespace Transacto.Plugins.ChartOfAccounts; 

internal class ChartOfAccounts : IPlugin {
	public string Name { get; } = nameof(ChartOfAccounts);

	public void Configure(IEndpointRouteBuilder builder) {
		builder.MapGet(string.Empty, ([FromServices] InMemoryProjectionDatabase database) => {
			var readModel = database.Get<ReadModel>();
			
			return Results.Hal(readModel.HasValue ? readModel.Value : ReadModel.None,
				readModel.HasValue ? readModel.Value.Checkpoint.ToCheckpoint() : Checkpoint.None,
				new ChartOfAccountsRepresentation());
		});

		builder.MapCommands(string.Empty,
			typeof(DefineAccount),
			typeof(RenameAccount),
			typeof(DeactivateAccount),
			typeof(ReactivateAccount));
	}

	public void ConfigureServices(IServiceCollection services) => services
		.AddCommandHandlerModule<ChartOfAccountsModule>()
		.AddInMemoryProjection<ReadModel>((readModel, e) => readModel with {
			ChartOfAccounts = e switch {
				AccountDefined d => readModel.ChartOfAccounts.SetItem(d.AccountNumber, new(d.AccountName)),
				AccountRenamed r => readModel.ChartOfAccounts.SetItem(r.AccountNumber,
					readModel.ChartOfAccounts[r.AccountNumber] with { AccountName = r.NewAccountName }),
				AccountDeactivated d => readModel.ChartOfAccounts.SetItem(d.AccountNumber,
					readModel.ChartOfAccounts[d.AccountNumber] with { Active = false }),
				AccountReactivated r => readModel.ChartOfAccounts.SetItem(r.AccountNumber,
					readModel.ChartOfAccounts[r.AccountNumber] with { Active = true }),
				_ => readModel.ChartOfAccounts
			}
		});

	public IEnumerable<Type> MessageTypes => Enumerable.Empty<Type>();

	private class ChartOfAccountsRepresentation : Hal<ReadModel>, IHalLinks<ReadModel>, IHalState<ReadModel> {
		public IEnumerable<Link> LinksFor(ReadModel resource) {
			foreach (var (accountNumber, (accountName, _)) in resource.ChartOfAccounts) {
				yield return new Link("self", $"chart-of-accounts/{accountNumber}", title: accountName);
			}
		}

		public object StateFor(ReadModel resource) => new SortedDictionary<string, string>(
			resource.ChartOfAccounts.ToDictionary(x => x.Key.ToString(), x => x.Value.AccountName));
	}

	private record ReadModel : MemoryReadModel {
		public static readonly ReadModel None = new();
		public ImmutableSortedDictionary<int, Account> ChartOfAccounts { get; init; } =
			ImmutableSortedDictionary<int, Account>.Empty;

		public record Account(string AccountName, bool Active = true);
	}
}
