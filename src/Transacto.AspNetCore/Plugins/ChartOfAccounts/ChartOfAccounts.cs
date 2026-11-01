using System.Collections.Immutable;
using Hallo;
using Microsoft.AspNetCore.Mvc;
using Transacto.Framework;
using Transacto.Framework.Http;
using Transacto.Framework.Http.Html;
using Transacto.Framework.Projections;
using Transacto.Infrastructure.EventStore;
using Transacto.Messages;

namespace Transacto.Plugins.ChartOfAccounts;

public class ChartOfAccounts : IPlugin {
	public static readonly LinkRelation SearchRelation = new("chart-of-accounts:search", "Search Chart of Accounts");

	public string Name { get; } = nameof(ChartOfAccounts);

	public void Configure(IEndpointRouteBuilder builder) {
		builder.MapGet("",
			([FromServices] InMemoryProjectionDatabase database,
				[FromServices] ChartOfAccountsRepresentation representation) => {
				return database.Get<ReadModel>() switch {
					Some<ReadModel> some => Results.Hal(some.Value, some.Value.Position.ToCheckpoint(), representation),
					_ => Results.Hal(ReadModel.None, Checkpoint.None, representation)
				};
			});

		builder.MapGet("/search",
			([FromQuery(Name = "t")] string? term,
				[FromQuery(Name = "at")] int[] accountTypes,
				[FromServices] InMemoryProjectionDatabase database,
				[FromServices] ChartOfAccountsSearchResultsRepresentation representation) => {
				if (database.Get<ReadModel>() is not Some<ReadModel> { Value: var readModel }) {
					return Results.Hal(ReadModel.None, Checkpoint.None, representation);
				}

				var query = readModel.ChartOfAccounts.Where(x => x.Value.Active);

				if (!string.IsNullOrWhiteSpace(term)) {
					query = query.Where(x =>
						x.Value.AccountName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
						x.Key.ToString().StartsWith(term.Trim(), StringComparison.Ordinal));
				}

				if (accountTypes.Length > 0) {
					query = query.Where(x => accountTypes.Any(at => StartsWithDigit(x.Key, at)));
				}

				var results = query.Take(25);

				return Results.Hal(results, readModel.Position.ToCheckpoint(), representation);

				static bool StartsWithDigit(int number, int targetDigit) {
					// Handle negative numbers
					int absolute = Math.Abs(number);

					// Reduce the number to its first single digit
					while (absolute >= 10) {
						absolute /= 10;
					}

					return absolute == targetDigit;
				}
			});

		builder.MapCommands(string.Empty,
			typeof(ConfigureChartOfAccounts),
			typeof(DefineAccount),
			typeof(RenameAccount),
			typeof(DeactivateAccount),
			typeof(ReactivateAccount)
		);
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
		.AddSingleton<ChartOfAccountsRepresentation>()
		.AddSingleton<ChartOfAccountsSearchResultsRepresentation>();

	public IEnumerable<Type> MessageTypes => [];

	public IEnumerable<LinkRelation> LinkRelations => [SearchRelation];

	private abstract class ChartOfAccountsRepresentationBase<T>(SiteRoot siteRoot) : Hal<T> {
		protected IEnumerable<Link> LinksFor() {
			yield return new Link("index", "../", title: "Home");
			yield return new Link("self", $"{siteRoot.Uri.AbsolutePath}", title: "Chart of Accounts");
			yield return new Link("index", $"{siteRoot.Uri.AbsolutePath}", title: "Chart of Accounts");
		}
	}

	private class ChartOfAccountsRepresentation(SiteRoot siteRoot)
		: ChartOfAccountsRepresentationBase<ReadModel>(siteRoot), IHalState<ReadModel>, IHalLinks<ReadModel> {
		private readonly SiteRoot _siteRoot = siteRoot;

		public object StateFor(ReadModel resource) => new SortedDictionary<string, AccountInfo>(
			resource.ChartOfAccounts.ToDictionary(x => x.Key.ToString(), x => x.Value), StringComparer.Ordinal);

		public IEnumerable<Link> LinksFor(ReadModel resource) {
			foreach (var link in LinksFor()) {
				yield return link;
			}

			foreach (var (accountNumber, (accountName, _)) in resource.ChartOfAccounts) {
				yield return new Link("self", $"{_siteRoot.Uri.AbsolutePath}/{accountNumber}",
					title: $"{accountNumber} - {accountName}");
			}
		}
	}

	private class ChartOfAccountsSearchResultsRepresentation(SiteRoot siteRoot)
		: ChartOfAccountsRepresentationBase<IEnumerable<KeyValuePair<int, AccountInfo>>>(siteRoot),
			IHalState<IEnumerable<KeyValuePair<int, AccountInfo>>>,
			IHalLinks<IEnumerable<KeyValuePair<int, AccountInfo>>> {
		public object StateFor(IEnumerable<KeyValuePair<int, AccountInfo>> resource) => new {
			items = resource.Select(x => new { key = x.Key, value = $"{x.Key} - {x.Value.AccountName}" }).ToArray()
		};

		public IEnumerable<Link> LinksFor(IEnumerable<KeyValuePair<int, AccountInfo>> resource) => LinksFor();
	}

	public record ReadModel : MemoryReadModel {
		public static readonly ReadModel None = new();

		public ImmutableSortedDictionary<int, AccountInfo> ChartOfAccounts { get; init; } =
			ImmutableSortedDictionary<int, AccountInfo>.Empty;
	}
}
