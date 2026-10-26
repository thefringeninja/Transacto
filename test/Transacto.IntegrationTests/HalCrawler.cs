using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Channels;

namespace Transacto;

internal class HalCrawler(HttpClient httpClient, string startUrl) {
	private readonly HashSet<Uri> _processedUris = [];
	private readonly Uri _baseUri = new UriBuilder {
		Path = startUrl,
	}.Uri;

	private readonly Channel<Uri> _channel = Channel.CreateUnbounded<Uri>(new UnboundedChannelOptions {
		SingleReader = false,
		SingleWriter = false
	});

	public async IAsyncEnumerable<string> CrawlAsync([EnumeratorCancellation] CancellationToken ct = default) {
		// 1. Initialize tracking and the channel queue
		var visited = new ConcurrentDictionary<string, bool>();

		// Seed the channel with the starting URL
		visited.TryAdd(_baseUri.AbsoluteUri, true);
		await _channel.Writer.WriteAsync(_baseUri, ct);
		yield return _baseUri.ToString();

		// Track active worker operations to know when the discovery queue is completely empty
		var activeWorkers = 0;

		// 2. Start parallel worker engines
		var workerTasks = new List<Task>();
		for (int i = 0; i < 3; i++) {
			workerTasks.Add(Task.Run(async () => {
				// Read continuous stream from the channel queue
				while (await _channel.Reader.WaitToReadAsync(ct)) {
					while (_channel.Reader.TryRead(out var currentUrl)) {
						Interlocked.Increment(ref activeWorkers);
						try {
							using var request = new HttpRequestMessage(HttpMethod.Get, currentUrl) {
								Headers = { Accept = { new MediaTypeWithQualityHeaderValue("application/hal+json") } }
							};

							var response = await httpClient.SendAsync(request, ct);
							if (response.Content.Headers.ContentType is not { MediaType: "application/hal+json" }) {
								continue;
							}

							var jsonContent = await response.Content.ReadAsStringAsync(ct);
							
							ExtractAndQueueLinks(jsonContent, currentUrl);
						} catch (HttpRequestException) {
						} finally {
							var remaining = Interlocked.Decrement(ref activeWorkers);

							// If channel is completely empty and no workers are fetching HTML, we are done
							if (remaining == 0 && _channel.Reader.Count == 0) {
								_channel.Writer.TryComplete();
							}
						}
					}
				}
			}, ct));
		}

		// 3. Yield found elements as they appear in the history map
		var yieldedCount = 1; // accounted for startUrl
		while (!_channel.Reader.Completion.IsCompleted || visited.Count > yieldedCount) {
			// Scan for elements added to history but not yet returned to the stream consumer
			foreach (var kvp in visited) {
				if (ct.IsCancellationRequested) break;

				// Yield back control safely to async stream consumer
				yield return kvp.Key;
				yieldedCount++;
			}

			if (_channel.Reader.Completion.IsCompleted && visited.Count == yieldedCount) {
				break;
			}

			// Small back-off delay to await more link extraction work
			await Task.Delay(50, ct);
		}

		// Ensure workers exit completely
		_channel.Writer.TryComplete();
		await Task.WhenAll(workerTasks);
	}


	private void ExtractAndQueueLinks(string jsonContent, Uri currentUrl) {
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

	private void ProcessLinkObject(JsonElement linkObject, Uri currentUrl) {
		if (!linkObject.TryGetProperty("href", out JsonElement hrefElement) ||
		    hrefElement.ValueKind != JsonValueKind.String) {
			return;
		}

		var href = hrefElement.GetString();
		if (string.IsNullOrEmpty(href)) {
			return;
		}

		// Combine with base URI to get an absolute URL
		var uri = new Uri(_baseUri, href);

		// Ensure it's within the same domain and hasn't been visited/queued
		if (!uri.AbsoluteUri.StartsWith(_baseUri.AbsoluteUri) ||
		    !_processedUris.Add(uri)) return;

		_channel.Writer.TryWrite(uri);
	}
}
