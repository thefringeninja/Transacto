using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Transacto.Integration {
	internal static class HttpClientExtensions {
		public static async Task SendCommand(this HttpClient client, string requestUri, object command,
			JsonSerializerOptions options,
			CancellationToken cancellationToken = default) {
			using var response = await client.PostAsync(requestUri, new MultipartFormDataContent {
				{new StringContent(command.GetType().Name), nameof(command)}, {
					new ReadOnlyMemoryContent(
						JsonSerializer.SerializeToUtf8Bytes(command, options)),
					"data", "data"
				}
			}, cancellationToken);
		}
	}
}
