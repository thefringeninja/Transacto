using System.Reflection;
using Microsoft.Extensions.FileProviders;

namespace Transacto.Framework.Http;

public static class ClientApp {
	/// <summary>
	/// Serves the front end from the build output on disk when it is present (development), falling back to the
	/// copy embedded in <paramref name="assembly"/>.
	/// </summary>
	public static IFileProvider FileProvider(Assembly assembly, string root = "dist") {
		var hasEmbeddedApp = assembly.GetManifestResourceNames()
			.Any(name => name.StartsWith(root + "/", StringComparison.Ordinal));
		IFileProvider embedded = hasEmbeddedApp
			? new ManifestEmbeddedFileProvider(assembly, root)
			: new NullFileProvider();
		var path = assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
			.FirstOrDefault(a => a.Key == "Transacto.ClientAppPath")?.Value;

		return path is not null && Directory.Exists(path)
			? new CompositeFileProvider(new PhysicalFileProvider(path), embedded)
			: embedded;
	}
}
