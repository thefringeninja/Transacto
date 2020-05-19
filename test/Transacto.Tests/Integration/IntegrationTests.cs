using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using SqlStreamStore;
using Transacto.Domain;
using Transacto.Framework;
using Transacto.Infrastructure;
using Transacto.Messages;

namespace Transacto.Integration {
	public abstract class IntegrationTests : IDisposable {
		private readonly TestServer _testServer;
		protected HttpClient HttpClient { get; }

		static IntegrationTests() {
			Inflector.Inflector.SetDefaultCultureFunc = () => new CultureInfo("en-US");
		}

		protected IntegrationTests() {
			_testServer = new TestServer(new WebHostBuilder()
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

		public void Dispose() {
			_testServer?.Dispose();
			HttpClient?.Dispose();
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
	}
}
