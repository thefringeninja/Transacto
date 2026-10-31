using System.Net;

namespace Transacto.Testing.Http;

public class HttpCentricTestSpecificationRunner(HttpClient client) : IHttpCentricTestSpecificationRunner {
	public async Task<HttpCentricTestResult> Run(HttpCentricTestSpecification specification) {
		ArgumentNullException.ThrowIfNull(specification);

		HttpRequest? request = null;
		HttpResponse? actual = null;

		for (var step = 0; step < specification.Messages.Length; step++) {
			switch (specification.Messages[step].Value) {
				case HttpRequest current:
					request = current;
					try {
						using var message = (HttpRequestMessage)current;
						using var response = await client.SendAsync(message);
						actual = response;
					} catch (Exception ex) {
						return new HttpCentricTestResult(specification, TestResultState.Failed, step, request,
							exception: ex);
					}

					// A request that is not followed by an expectation is a When: it must succeed.
					if (!IsFollowedByResponse(specification, step) && actual.StatusCode is < (HttpStatusCode)200 or >= (HttpStatusCode)300) {
						return new HttpCentricTestResult(specification, TestResultState.Failed, step, request,
							actual: actual);
					}

					break;
				case HttpResponse expected:
					if (actual is null || !expected.Equals(actual)) {
						return new HttpCentricTestResult(specification, TestResultState.Failed, step, request,
							expected, actual);
					}

					break;
				case HttpWait wait:
					try {
						await Task.Delay(wait.Delay, wait.CancellationToken);
					} catch (Exception ex) {
						return new HttpCentricTestResult(specification, TestResultState.Failed, step, request,
							exception: ex);
					}

					break;
			}
		}

		return new HttpCentricTestResult(specification, TestResultState.Passed);
	}

	private static bool IsFollowedByResponse(HttpCentricTestSpecification specification, int step) =>
		step + 1 < specification.Messages.Length && specification.Messages[step + 1].Value is HttpResponse;
}
