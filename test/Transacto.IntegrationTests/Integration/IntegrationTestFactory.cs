using KurrentDB.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;
using SqlStreamStore;

/*
namespace Transacto.Integration;

internal class IntegrationTestFactory : WebApplicationFactory<IntegrationTestFactory> {
#pragma warning disable CS7022 // The entry point of the program is global code; ignoring entry point
	public static async Task<int> Main(string[] args) {
#pragma warning restore CS7022 // The entry point of the program is global code; ignoring entry point
		await using var KurrentDBClient = new KurrentDBClient(new KurrentDBClientSettings {
			ConnectivitySettings = {
				Address = new UriBuilder {
					Port = 2113
				}.Uri,
				Insecure = true
			},
			OperationOptions = {
				ThrowOnAppendFailure = true
			},
			CreateHttpMessageHandler = () => new SocketsHttpHandler {
				SslOptions = {
					RemoteCertificateValidationCallback = delegate {
						return true;
					}
				}
			}
		});
		using var streamStoreClient = new HttpClientSqlStreamStore(new HttpClientSqlStreamStoreSettings {
			CreateHttpClient = () => new HttpClient(new SocketsHttpHandler {
				SslOptions = {
					RemoteCertificateValidationCallback = delegate {
						return true;
					}
				}
			}, true)
		});

		var builder = WebApplication.CreateBuilder(args);
		builder.Services.AddTransacto(KurrentDBClient, new NpgsqlConnectionStringBuilder(), streamStoreClient);

		var app = builder.Build();
		app.UseTransacto();

		await app.RunAsync();

		return 0;
	}
}
*/
