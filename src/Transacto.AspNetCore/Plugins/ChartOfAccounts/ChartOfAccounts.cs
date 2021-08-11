using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
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
				var readModel = context.RequestServices.GetRequiredService<ReadModel>();

				var response = new HalResponse(context.Request, new ChartOfAccountsRepresentation(),
					readModel.Checkpoint, readModel);
				if (response.StatusCode != HttpStatusCode.NotAcceptable) {
					response.StatusCode = HttpStatusCode.OK;
				}

				return new ValueTask<Response>(response);
			})
			.MapCommands(string.Empty,
				typeof(DefineAccount),
				typeof(RenameAccount),
				typeof(DeactivateAccount),
				typeof(ReactivateAccount));

		public void ConfigureServices(IServiceCollection services) => services
			.AddCommandHandlerModule<ChartOfAccountsModule>()
			.AddInMemoryProjection<ReadModel>(new ChartOfAccountsProjection());

		public IEnumerable<Type> MessageTypes => Enumerable.Empty<Type>();

		private class ChartOfAccountsProjection : InMemoryProjection<ReadModel> {
			public ChartOfAccountsProjection() {
				When<AccountDefined>((readModel, e) =>
					readModel.AccountDefined(e.AccountNumber, e.AccountName));
				When<AccountDeactivated>((readModel, e) =>
					readModel.AccountActivationChanged(e.AccountNumber, false));
				When<AccountReactivated>((readModel, e) =>
					readModel.AccountActivationChanged(e.AccountNumber, true));
				When<AccountRenamed>((readModel, e) =>
					readModel.AccountRenamed(e.AccountNumber, e.NewAccountName));
			}
		}

		private class ChartOfAccountsRepresentation : Hal<ReadModel>, IHalLinks<ReadModel>, IHalState<ReadModel> {
			public IEnumerable<Link> LinksFor(ReadModel resource) {
				foreach (var (accountNumber, (accountName, _)) in resource.ChartOfAccounts) {
					yield return new Link("self", $"chart-of-accounts/{accountNumber}", title: accountName);
				}
			}

			public object StateFor(ReadModel resource) =>
				new SortedDictionary<string, string>(resource.ChartOfAccounts.ToDictionary(x => x.Key.ToString(),
					x => x.Value.accountName));
		}

		private class ReadModel : IMemoryReadModel {
			private ImmutableSortedDictionary<int, (string accountName, bool active)> _chartOfAccounts;

			public ImmutableSortedDictionary<int, (string accountName, bool active)> ChartOfAccounts =>
				_chartOfAccounts;

			public Optional<Position> Checkpoint { get; set; }

			public ReadModel() {
				_chartOfAccounts = ImmutableSortedDictionary<int, (string accountName, bool active)>.Empty;
			}

			public void AccountDefined(int accountNumber, string accountName) =>
				Interlocked.Exchange(ref _chartOfAccounts,
					_chartOfAccounts.SetItem(accountNumber, (accountName, true)));

			public void AccountRenamed(int accountNumber, string accountName) {
				if (!_chartOfAccounts.TryGetValue(accountNumber, out var x)) {
					return;
				}

				Interlocked.Exchange(ref _chartOfAccounts,
					_chartOfAccounts.SetItem(accountNumber, (accountName, x.active)));
			}

			public void AccountActivationChanged(int accountNumber, bool active) {
				if (!_chartOfAccounts.TryGetValue(accountNumber, out var x)) {
					return;
				}

				Interlocked.Exchange(ref _chartOfAccounts,
					_chartOfAccounts.SetItem(accountNumber, (x.accountName, active)));
			}
		}
	}
}
