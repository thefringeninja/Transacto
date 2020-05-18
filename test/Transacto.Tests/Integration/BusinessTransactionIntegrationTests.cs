using System;
using System.Net.Http;
using System.Threading.Tasks;
using EventStore.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using SqlStreamStore;
using Transacto.Domain;
using Transacto.Framework;
using Transacto.Infrastructure;
using Transacto.Messages;
using Xunit;

namespace Transacto.Integration {
	public class BusinessTransactionIntegrationTests : IDisposable {
		private readonly TestServer _testServer;
		private readonly HttpClient _httpClient;

		public BusinessTransactionIntegrationTests() {
			_testServer = new TestServer(new WebHostBuilder()
				.Configure(app => app.UseTransacto().Map("/transactions", inner => inner.UseRouting().UseEndpoints(
					e => e.MapBusinessTransaction<BusinessTransaction>(string.Empty))))
				.ConfigureServices(s => s
					.AddEventStoreClient(settings => {
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
						BaseAddress = new UriBuilder {Port = 5002}.Uri
					}))
					.AddTransacto(
						MessageTypeMapper.Create(
							new MessageTypeMapper(new[] {typeof(BusinessTransaction)})))
					.AddHostedService(provider => new StreamStoreProjectionHost(
						provider.GetRequiredService<EventStoreClient>(),
						provider.GetRequiredService<IMessageTypeMapper>(),
						provider.GetRequiredService<IStreamStore>(),
						new BusinessTransactionFeed(provider.GetRequiredService<IMessageTypeMapper>())))));
			_httpClient = _testServer.CreateClient();
		}

		[Fact]
		public async Task Somewthing() {
			var now = DateTimeOffset.UtcNow;
			var period = new PeriodIdentifier(now.Month, now.Year);
			var transactionId = Guid.NewGuid();
			await _httpClient.SendCommand("/transactions", new PostGeneralLedgerEntry {
				BusinessTransaction = new BusinessTransaction {
					TransactionId = transactionId,
					ReferenceNumber = 1,
					Version = 1
				},
				Period = period.ToDto(),
				CreatedOn = now,
				GeneralLedgerEntryId = transactionId
			}, TransactoSerializerOptions.CommandSerializerOptions(typeof(BusinessTransaction)));
			await Task.Delay(TimeSpan.FromMinutes(5));
		}

		public void Dispose() {
			_httpClient?.Dispose();
			_testServer?.Dispose();
		}

		private class BusinessTransactionEntry : FeedEntry {
			public Guid TransactonId { get; set; }
			public int ReferenceNumber { get; set; }
		}

		private class BusinessTransactionFeed : StreamStoreFeedProjection<BusinessTransactionEntry> {
			public BusinessTransactionFeed(IMessageTypeMapper messageTypeMapper) : base("businessTransactions", messageTypeMapper) {
				When<BusinessTransaction>((e, _) => new BusinessTransactionEntry {
					TransactonId = e.TransactionId,
					ReferenceNumber = e.ReferenceNumber
				});
			}
		}
	}
}
