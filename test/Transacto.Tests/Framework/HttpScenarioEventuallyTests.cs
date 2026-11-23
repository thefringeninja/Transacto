using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.Net.Http.Headers;
using Transacto.Testing.Http;
using HttpRequest = Transacto.Testing.Http.HttpRequest;
using HttpResponse = Transacto.Testing.Http.HttpResponse;

namespace Transacto.Framework;

public class HttpScenarioEventuallyTests {
	private static readonly HttpRequest Request = new(HttpMethod.Get, "/projection");

	private static readonly HttpResponse Ready = new(HttpStatusCode.OK, "OK",
		new ResponseHeaders(new HeaderDictionary()) {
			ContentType = new MediaTypeHeaderValue("text/plain")
		}, [.. "ready"u8]);

	public async Task retries_until_the_response_matches() {
		var handler = new StubHandler(notReadyResponses: 2);

		var result = await Run(handler, new HttpScenario()
			.ThenEventually(Request, Ready, TimeSpan.FromSeconds(5)));

		Assert.True(result.Passed);
		Assert.Equal(3, handler.Requests);
	}

	public async Task fails_with_the_last_response_once_the_timeout_passes() {
		var handler = new StubHandler(notReadyResponses: int.MaxValue);

		var result = await Run(handler, new HttpScenario()
			.ThenEventually(Request, Ready, TimeSpan.FromMilliseconds(300)));

		Assert.True(result.Failed);
		Assert.Equal(Ready, result.Expected);
		Assert.Equal(HttpStatusCode.NotFound, result.Actual?.StatusCode);
		Assert.True(handler.Requests > 1);
	}

	public async Task then_does_not_retry() {
		var handler = new StubHandler(notReadyResponses: 1);

		var result = await Run(handler, new HttpScenario().Then(Request, Ready));

		Assert.True(result.Failed);
		Assert.Equal(1, handler.Requests);
	}

	public async Task continues_with_later_steps_after_an_eventual_match() {
		var handler = new StubHandler(notReadyResponses: 1);

		var result = await Run(handler, new HttpScenario()
			.ThenEventually(Request, Ready, TimeSpan.FromSeconds(5))
			.Then(Request, Ready));

		Assert.True(result.Passed);
	}

	public async Task retries_through_transient_exceptions() {
		var handler = new StubHandler(notReadyResponses: 1, throwing: true);

		var result = await Run(handler, new HttpScenario()
			.ThenEventually(Request, Ready, TimeSpan.FromSeconds(5)));

		Assert.True(result.Passed);
		Assert.Equal(2, handler.Requests);
	}

	private static async Task<HttpCentricTestResult> Run(StubHandler handler, IHttpCentricTestSpecificationBuilder builder) {
		using var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
		return await new HttpCentricTestSpecificationRunner(client).Run(builder.Build());
	}

	private class StubHandler(int notReadyResponses, bool throwing = false) : HttpMessageHandler {
		public int Requests { get; private set; }

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
			CancellationToken cancellationToken) =>
			++Requests <= notReadyResponses && throwing
				? throw new HttpRequestException("connection reset")
				: Task.FromResult(Requests <= notReadyResponses
				? new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent("missing") }
				: new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("ready") });
	}
}
