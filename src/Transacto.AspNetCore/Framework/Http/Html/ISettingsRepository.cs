namespace Transacto.Framework.Http.Html;

public interface ISettingsRepository {
	public Task SaveAsync(FormatSettings settings, ulong expectedVersion, CancellationToken cancellationToken);
}