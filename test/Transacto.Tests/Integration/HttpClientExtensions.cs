using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using Transacto.Framework;

namespace Transacto.Integration {
	internal static class HttpClientExtensions {
		public static async Task<Checkpoint> SendCommand(this HttpClient client, string requestUri, object command,
			JsonSerializerOptions options,
			CancellationToken cancellationToken = default) {
			using var response = await client.PostAsync(requestUri, new MultipartFormDataContent {
				{new StringContent(command.GetType().Name), nameof(command)}, {
					new ReadOnlyMemoryContent(
						JsonSerializer.SerializeToUtf8Bytes(command, options)),
					"data", "data"
				}
			}, cancellationToken);

			var value = (await response.Content.ReadAsStringAsync(cancellationToken)).Trim();

			return Checkpoint.FromString(value);
		}

		public static Task<HttpResponseMessage> ConditionalGetAsync(this HttpClient client, string requestUri,
			Checkpoint checkpoint, CancellationToken cancellationToken = default) =>
			Policy.Handle<HttpRequestException>()
				.WaitAndRetryAsync(5, count => TimeSpan.FromMilliseconds(count * 2 * 100))
				.ExecuteAsync(async ct => {
					using var request = new HttpRequestMessage(HttpMethod.Get, requestUri) {
						Headers = {IfMatch = {new EntityTagHeaderValue($@"""{checkpoint}""")}}
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
