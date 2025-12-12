using System.Text.Json;
using KurrentDB.Client;
using Transacto.Framework.Http.Html;

namespace Transacto.Infrastructure.EventStore;

public class EventStoreSettingsRepository(KurrentDBClient client) : ISettingsRepository {
	public async Task SaveAsync(FormatSettings settings, ulong expectedVersion, CancellationToken cancellationToken) {
		await client.AppendToStreamAsync("formatSettings", StreamState.StreamRevision(expectedVersion), [
			new EventData(Uuid.NewUuid(), "formatSettings",
				JsonSerializer.SerializeToUtf8Bytes(settings, FormatSettingsContext.Default.FormatSettings))
		], cancellationToken: cancellationToken);
	}
}
