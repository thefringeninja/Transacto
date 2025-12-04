using System.Net.Http.Headers;
using System.Text.Json;

namespace Transacto;

internal class HalCrawler {
	private readonly HttpClient _httpClient;
	private readonly HashSet<string> _visitedUrls = new();
	private readonly Queue<string> _urlsToVisit = new();
	private readonly List<string> _allCollectedLinks = [];
	private readonly Uri _baseUri;

	public HalCrawler(HttpClient httpClient, string startUrl) {
		_httpClient = httpClient;
		_baseUri = new UriBuilder {
			Path = startUrl,
		}.Uri;
		_urlsToVisit.Enqueue(_baseUri.ToString());
	}

	public async Task<string[]> CrawlAsync(CancellationToken ct = default) {
		while (_urlsToVisit.Count > 0) {
			var currentUrl = new UriBuilder(_urlsToVisit.Dequeue()).Uri.PathAndQuery;

			// Skip if already visited
			if (!_visitedUrls.Add(currentUrl)) {
				continue;
			}

			using var request = new HttpRequestMessage(HttpMethod.Get, currentUrl) {
				Headers = { Accept = { new MediaTypeWithQualityHeaderValue("application/hal+json") } }
			};

			var response = await _httpClient.SendAsync(request, ct);
			if (response.Content.Headers.ContentType is not { MediaType: "application/hal+json" }) {
				continue;
			}

			var jsonContent = await response.Content.ReadAsStringAsync(ct);

			// Parse the JSON and extract links
			ExtractAndQueueLinks(jsonContent, currentUrl);
			_allCollectedLinks.Add(currentUrl); // Add the resource URL itself to the list
		}

		return _allCollectedLinks.ToArray();
	}

	private void ExtractAndQueueLinks(string jsonContent, string currentUrl) {
		using JsonDocument doc = JsonDocument.Parse(jsonContent);
		var root = doc.RootElement;

		// Extract links from the "_links" property
		if (!root.TryGetProperty("_links", out JsonElement linksElement)) {
			return;
		}

		foreach (var linkProperty in linksElement.EnumerateObject()) {
			switch (linkProperty.Value.ValueKind) {
				// Links can be a single object or an array of objects
				case JsonValueKind.Array: {
					foreach (var linkObject in linkProperty.Value.EnumerateArray()) {
						ProcessLinkObject(linkObject, currentUrl);
					}

					break;
				}
				case JsonValueKind.Object:
					ProcessLinkObject(linkProperty.Value, currentUrl);
					break;
			}
		}
	}

	private void ProcessLinkObject(JsonElement linkObject, string currentUrl) {
		if (!linkObject.TryGetProperty("href", out JsonElement hrefElement) ||
		    hrefElement.ValueKind != JsonValueKind.String) {
			return;
		}
		var href = hrefElement.GetString();
		if (string.IsNullOrEmpty(href)) {
			return;
		}
		// Combine with base URI to get an absolute URL
		var absoluteUri = new Uri(_baseUri, href).AbsoluteUri;

		// Ensure it's within the same domain and hasn't been visited/queued
		if (!absoluteUri.StartsWith(_baseUri.AbsoluteUri) ||
		    _visitedUrls.Contains(absoluteUri) ||
		    _urlsToVisit.Contains(absoluteUri)) return;

		_urlsToVisit.Enqueue(absoluteUri);
	}
}
