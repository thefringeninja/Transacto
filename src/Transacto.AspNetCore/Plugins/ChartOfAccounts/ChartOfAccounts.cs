using System.Collections.Immutable;
using Hallo;
using Microsoft.AspNetCore.Mvc;
using Transacto.Framework;
using Transacto.Framework.Http;
using Transacto.Framework.Projections;
using Transacto.Infrastructure.EventStore;
using Transacto.Messages;

namespace Transacto.Plugins.ChartOfAccounts;

internal class ChartOfAccounts : IPlugin {
	public string Name { get; } = nameof(ChartOfAccounts);

	public void Configure(IEndpointRouteBuilder builder) {
		builder.MapGet(string.Empty,
			([FromServices] InMemoryProjectionDatabase database,
					[FromServices] ChartOfAccountsRepresentation representation) => {
				return database.Get<ReadModel>() switch {
					{ HasValue: true } optional => Results.Hal(optional.Value,
						optional.Value.Checkpoint.ToCheckpoint(), representation),
					_ => Results.Hal(ReadModel.None, Checkpoint.None, representation)
				};
			});

		builder.MapCommands(string.Empty,
		[
			typeof(ConfigureChartOfAccounts),
			typeof(DefineAccount),
			typeof(RenameAccount),
			typeof(DeactivateAccount),
			typeof(ReactivateAccount)
		]);
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
		})
		.AddSingleton<ChartOfAccountsRepresentation>();

	public IEnumerable<Type> MessageTypes => [];

	private class ChartOfAccountsRepresentation(SiteRoot siteRoot)
		: Hal<ReadModel>, IHalLinks<ReadModel>, IHalState<ReadModel> {
		public IEnumerable<Link> LinksFor(ReadModel resource) {
			foreach (var (accountNumber, (accountName, _)) in resource.ChartOfAccounts) {
				yield return new Link("self", $"{siteRoot.Uri.AbsolutePath}/{accountNumber}", title: accountName);
			}
		}

		public object StateFor(ReadModel resource) => new SortedDictionary<string, AccountInfo>(
			resource.ChartOfAccounts.ToDictionary(x => x.Key.ToString(), x => x.Value));
	}

	public record ReadModel : MemoryReadModel {
		public static readonly ReadModel None = new();

		public ImmutableSortedDictionary<int, AccountInfo> ChartOfAccounts { get; init; } =
			ImmutableSortedDictionary<int, AccountInfo>.Empty;
	}
}

public record AccountInfo(string AccountName, bool Active = true);
