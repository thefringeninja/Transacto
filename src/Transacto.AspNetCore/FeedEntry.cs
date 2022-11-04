using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Transacto; 

public abstract class FeedEntry {
	[JsonPropertyName("_events")] public List<string> Events { get; } = new();
}