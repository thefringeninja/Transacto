using System.Net;
using System.Net.Http.Headers;
using Polly;
using Transacto.Framework;

namespace Transacto.Testing.Http;

public class HttpCentricTestSpecificationRunner(HttpClient client) : IHttpCentricTestSpecificationRunner {
	public async Task<HttpCentricTestResult> Run(HttpCentricTestSpecification specification) {
		ArgumentNullException.ThrowIfNull(specification);

		HttpRequest? request = null;
		HttpResponse? actual = null;
		Checkpoint? checkpoint = null;

		for (var step = 0; step < specification.Messages.Length; step++) {
			switch (specification.Messages[step].Value) {
				case HttpRequest current:
					request = current;
					try {
						actual = await Send(current, checkpoint);
					} catch (Exception ex) {
						return new HttpCentricTestResult(specification, TestResultState.Failed, step, request,
							exception: ex);
					}

					if (current.Method == HttpMethod.Post && actual.StatusCode == HttpStatusCode.OK &&
					    TryParseCheckpoint(actual, out var parsed)) {
						checkpoint = parsed;
					}

					break;
				case HttpResponse expected:
					if (actual is null || !expected.Equals(actual)) {
						return new HttpCentricTestResult(specification, TestResultState.Failed, step, request,
							expected, actual);
					}

					break;
			}
		}

		return new HttpCentricTestResult(specification, TestResultState.Passed);
	}

	private async Task<HttpResponse> Send(HttpRequest request, Checkpoint? checkpoint) {
		if (checkpoint is not { } value || request.Method != HttpMethod.Get) {
			using var message = (HttpRequestMessage)request;
			using var response = await client.SendAsync(message);
			return await ToResponse(response);
		}

		using var retried = await Policy
			.HandleResult<HttpResponseMessage>(message => message.StatusCode == HttpStatusCode.PreconditionFailed)
			.WaitAndRetryAsync(5, count => TimeSpan.FromMilliseconds(count * 2 * 100),
				(result, _, _) => result.Result.Dispose())
			.ExecuteAsync(async ct => {
				using var message = (HttpRequestMessage)request;
				message.Headers.IfMatch.Add(new EntityTagHeaderValue($"\"{value}\""));
				return await client.SendAsync(message, ct);
			}, CancellationToken.None);
		return await ToResponse(retried);
	}

	private static async Task<HttpResponse> ToResponse(HttpResponseMessage response) {
		// materialize body before the message is disposed
		await response.Content.LoadIntoBufferAsync();
		return response;
	}

	private static bool TryParseCheckpoint(HttpResponse response, out Checkpoint checkpoint) {
		checkpoint = default;
		if (response.Body is not { Length: > 0 } body) {
			return false;
		}

		var text = System.Text.Encoding.UTF8.GetString(body).Trim();
		if (text.Length == 0 || text.Length % 2 != 0 || !text.All(Uri.IsHexDigit)) {
			return false;
		}

		checkpoint = Checkpoint.FromString(text);
		return true;
	}
}
