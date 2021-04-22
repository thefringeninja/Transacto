using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Services;
using EventStore.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Retry;
using SqlStreamStore;
using Transacto.Domain;
using Transacto.Messages;
using Xunit;

namespace Transacto.Integration {
	[Collection(nameof(IntegrationTests))]
	public abstract class IntegrationTests : IDisposable, IAsyncLifetime {
		private readonly IContainerService _eventStore;
		private readonly IContainerService _streamStore;

		private TestServer _testServer;
		protected HttpClient HttpClient { get; private set; }
		protected EventStoreClient EventStoreClient => _testServer.Services.GetRequiredService<EventStoreClient>();

		static IntegrationTests() {
			Inflector.Inflector.SetDefaultCultureFunc = () => new CultureInfo("en-US");
		}

		protected IntegrationTests() {
			HttpClient = null!;
			_testServer = null!;
			_eventStore = new Builder()
				.UseContainer()
				.WithName("transacto-es-test")
				.UseImage("eventstore/eventstore:21.2.0-buster-slim")
				.ReuseIfExists()
				.ExposePort(2113, 2113)
				.WithEnvironment("EVENTSTORE_ENABLE_ATOM_PUB_OVER_HTTP=true",
					"EVENTSTORE_INSECURE=true")
				.Build();
			_streamStore = new Builder()
				.UseContainer()
				.WithName("transacto-sss-test")
				.UseImage("sqlstreamstore/server:1.2.0-beta.5-alpine3.9")
				.ReuseIfExists()
				.ExposePort(5000, 80)
				.Build();
		}

		public void Dispose() {
			HttpClient?.Dispose();
			_testServer?.Dispose();
			_streamStore?.Dispose();
			_eventStore?.Dispose();
		}

		protected async IAsyncEnumerable<(AccountNumber accountNumber, AccountName accountName)>
			OpenBooks(DateTimeOffset now) {
			await HttpClient.SendCommand("/general-ledger", new OpenGeneralLedger {
				OpenedOn = now
			}, TransactoSerializerOptions.Commands);
			foreach (var (accountNumber, accountName) in GetChartOfAccounts().OrderBy(_ => Guid.NewGuid())) {
				await HttpClient.SendCommand("/chart-of-accounts", new DefineAccount {
					AccountName = accountName.ToString(),
					AccountNumber = accountNumber.ToInt32()
				}, TransactoSerializerOptions.BusinessTransactions());
				yield return (accountNumber, accountName);
			}

			await Task.Delay(TimeSpan.FromMilliseconds(500));
		}

		private static IEnumerable<(AccountNumber accountNumber, AccountName accountName)> GetChartOfAccounts() {
			yield return (new AccountNumber(1000), new AccountName("Bank Checking Account"));
			yield return (new AccountNumber(1050), new AccountName("Bank Savings Account"));
			yield return (new AccountNumber(1200), new AccountName("Accounts Receivable"));
			yield return (new AccountNumber(2000), new AccountName("Accounts Payable"));
			yield return (new AccountNumber(3000), new AccountName("Opening Balance Equity"));
			yield return (new AccountNumber(3900), new AccountName("Retained Earnings"));
			yield return (new AccountNumber(4000), new AccountName("Sales Income"));
			yield return (new AccountNumber(5000), new AccountName("Cost of Goods Sold"));
		}

		public async Task InitializeAsync() {
			_eventStore.Start();
			_streamStore.Start();

			using var client = new HttpClient(new SocketsHttpHandler {
				SslOptions = {
					RemoteCertificateValidationCallback = delegate {
						return true;
					}
				}
			}, true);

			await Retry.ExecuteAsync(async () => {
				using var response = await client.GetAsync("http://localhost:2113/");
				if (response.StatusCode >= HttpStatusCode.BadRequest) {
					throw new Exception();
				}
			});
			await Retry.ExecuteAsync(async () => {
				using var response =
					await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://localhost:5000/") {
						Headers = {Accept = {new MediaTypeWithQualityHeaderValue("application/hal+json")}}
					});
				if (response.StatusCode >= HttpStatusCode.BadRequest) {
					throw new Exception();
				}
			});

			_testServer = new TestServer(new WebHostBuilder()
				.ConfigureServices(s => s
					.AddEventStoreClient(settings => {
						settings.ConnectivitySettings.Address = new UriBuilder {
							Port = 2113
						}.Uri;
						settings.ConnectivitySettings.Insecure = true;
						settings.OperationOptions.ThrowOnAppendFailure = true;
						settings.CreateHttpMessageHandler = () => new SocketsHttpHandler {
							SslOptions = {
								RemoteCertificateValidationCallback = delegate {
									return true;
								}
							}
						};
					})
					.AddSingleton<IStreamStore>(new HttpClientSqlStreamStore(new HttpClientSqlStreamStoreSettings {
						CreateHttpClient = () => new HttpClient(new SocketsHttpHandler {
							SslOptions = {
								RemoteCertificateValidationCallback = delegate {
									return true;
								}
							}
						}, true)
					}))
					.AddTransacto())
				.Configure(app => app.UseTransacto()));
			HttpClient = _testServer.CreateClient();
		}

		private static AsyncRetryPolicy Retry => Policy
			.Handle<Exception>()
			.WaitAndRetryAsync(100, i => TimeSpan.FromMilliseconds(Math.Pow(i, 2)));

		public Task DisposeAsync() {
			Dispose();
			return Task.CompletedTask;
		}
	}
}
