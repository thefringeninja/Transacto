using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client;
using Polly;

namespace Transacto.Integration {
	internal static class HttpClientExtensions {
		public static async Task<Position> SendCommand(this HttpClient client, string requestUri, object command,
			JsonSerializerOptions options,
			CancellationToken cancellationToken = default) {
			var separator = new[] {'/'};

			using var response = await client.PostAsync(requestUri, new MultipartFormDataContent {
				{new StringContent(command.GetType().Name), nameof(command)}, {
					new ReadOnlyMemoryContent(
						JsonSerializer.SerializeToUtf8Bytes(command, options)),
					"data", "data"
				}
			}, cancellationToken);

			var value = await response.Content.ReadAsStringAsync();

			var parts = value?.Split(separator, 2) ?? Array.Empty<string>();

			if (parts.Length != 2 || !ulong.TryParse(parts[0], out var p) || !ulong.TryParse(parts[1], out var c)) {
				return Position.Start;
			}

			return new Position(p, c);
		}

		public static Task<HttpResponseMessage> ConditionalGetAsync(this HttpClient client, string requestUri,
			Position position, CancellationToken cancellationToken = default) =>
			Policy.Handle<HttpRequestException>()
				.WaitAndRetryAsync(5, count => TimeSpan.FromMilliseconds(count * 2 * 100))
				.ExecuteAsync(async ct => {
					using var request = new HttpRequestMessage(HttpMethod.Get, requestUri) {
						Headers = {
							IfMatch = {
								new EntityTagHeaderValue($@"""{position.CommitPosition}/{position.PreparePosition}""")
							}
						}
					};
					var response = await client.SendAsync(request, ct);
					try {
						response.EnsureSuccessStatusCode();
						return response;
					} catch (HttpRequestException) {
						response.Dispose();
						throw;
					}
				}, cancellationToken);
	}
}
