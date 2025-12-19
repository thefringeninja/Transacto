using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Hallo;
using Polly;
using Transacto;
using Transacto.Framework;

// ReSharper disable once CheckNamespace
namespace System.Net;

internal static class HttpClientExtensions {
	extension(HttpClient client) {
		public async Task<Checkpoint> SendCommand(string requestUri, object command,
			JsonSerializerOptions options, CancellationToken cancellationToken = default) {
			using var response = await client.PostAsync(requestUri, new MultipartFormDataContent {
				{ new StringContent(command.GetType().Name), nameof(command) }, {
					new ReadOnlyMemoryContent(
						JsonSerializer.SerializeToUtf8Bytes(command, options)),
					"data", "data"
				}
			}, cancellationToken);

			var value = (await response.Content.ReadAsStringAsync(cancellationToken)).Trim();

			if (!response.IsSuccessStatusCode) {
				throw new HttpRequestException(HttpRequestError.InvalidResponse, value,
					statusCode: response.StatusCode);
			}

			return Checkpoint.FromString(value);
		}

		public async Task<HalResponse<T>> ConditionalGetAsync2<T>(string requestUri,
			Checkpoint checkpoint, CancellationToken cancellationToken = default) {
			using var response = await Policy
				.HandleResult<HttpResponseMessage>(message => message.StatusCode == HttpStatusCode.PreconditionFailed)
				.WaitAndRetryAsync(5, count => TimeSpan.FromMilliseconds(count * 2 * 100),
					(result, _, _) => result.Result.Dispose())
				.ExecuteAsync(async ct => {
					using var request = new HttpRequestMessage(HttpMethod.Get, requestUri) {
						Headers = {
							IfMatch = {
	w							new EntityTagHeaderValue($"""
								                          "{checkpoint}"
								                          """)
							}
						}
					};
					return await client.SendAsync(request, ct);
				}, cancellationToken);

			var x = await response.Content.ReadAsStringAsync(cancellationToken);
			var resource = JsonSerializer.Deserialize<T>(x);

			var links = (JsonNode.Parse(x) as JsonObject).ParseLinks();

			return new HalResponse<T>(resource!, links, response.StatusCode);
		}

		public Task<HttpResponseMessage> ConditionalGetAsync(string requestUri,
			Checkpoint checkpoint, CancellationToken cancellationToken = default) =>
			Policy.Handle<HttpRequestException>()
				.WaitAndRetryAsync(5, count => TimeSpan.FromMilliseconds(count * 2 * 100))
				.ExecuteAsync(async ct => {
					using var request = new HttpRequestMessage(HttpMethod.Get, requestUri) {
						Headers = {
							IfMatch = {
								new EntityTagHeaderValue($"""
								                          "{checkpoint}"
								                          """)
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

	extension(JsonObject? jsonObject) {
		public Link[] ParseLinks() {
			var links = new List<Link>();

			if (jsonObject == null || !jsonObject.TryGetPropertyValue("_links", out var linksNode) ||
			    linksNode is not JsonObject linksObject) {
				return links.ToArray();
			}

			foreach (var (rel, property) in linksObject) {
				switch (property) {
					// Handle both single link object and array of links
					case JsonObject singleLink: {
						var link = ParseLink(rel, singleLink);
						if (!link.HasValue) {
							continue;
						}

						links.Add(link.Value);
						break;
					}
					case JsonArray linkArray: {
						foreach (var item in linkArray) {
							if (item is not JsonObject arrayLink) {
								continue;
							}

							var link = ParseLink(rel, arrayLink);
							if (!link.HasValue) {
								continue;
							}

							links.Add(link.Value);
						}

						break;
					}
				}
			}

			return links.ToArray();
		}

		private static Link? ParseLink(string rel, JsonObject linkObject) {
			if (!linkObject.TryGetPropertyValue("href", out var hrefNode) ||
			    hrefNode is not JsonValue jsonHref) {
				return null;
			}

			// Extract optional properties with null checks
			linkObject.TryGetPropertyValue("type", out var typeNode);
			linkObject.TryGetPropertyValue("deprecation", out var deprecationNode);
			linkObject.TryGetPropertyValue("name", out var nameNode);
			linkObject.TryGetPropertyValue("profile", out var profileNode);
			linkObject.TryGetPropertyValue("title", out var titleNode);
			linkObject.TryGetPropertyValue("hreflang", out var hrefLangNode); // Note lowercase 'l' in JSON spec

			var deprecationUri = deprecationNode is JsonValue
				? new Uri(deprecationNode.GetValue<string>(), UriKind.RelativeOrAbsolute)
				: null;
			var profileUri = profileNode is JsonValue
				? new Uri(profileNode.GetValue<string>(), UriKind.RelativeOrAbsolute)
				: null;

			return new Link(
				rel: rel,
				href: jsonHref.GetValue<string>(),
				type: typeNode?.GetValue<string>(),
				deprecation: deprecationUri,
				name: nameNode?.GetValue<string>(),
				profile: profileUri,
				title: titleNode?.GetValue<string>(),
				hrefLang: hrefLangNode?.GetValue<string>()
			);
		}
	}
}
