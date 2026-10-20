namespace Transacto.Framework.Http.Html;

public interface ISettingsRepository {
	public Task SaveAsync(Formatter settings, ulong expectedVersion, CancellationToken cancellationToken);
}