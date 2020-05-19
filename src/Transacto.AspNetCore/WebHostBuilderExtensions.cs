using Microsoft.Extensions.DependencyInjection;

// ReSharper disable CheckNamespace
namespace Microsoft.AspNetCore.Hosting {
// ReSharper restore CheckNamespace

    internal static class WebHostBuilderExtensions {
        public static IWebHostBuilder UseStartup(this IWebHostBuilder builder, IStartup startup)
            => builder
                .ConfigureServices(services => services.AddSingleton(startup));
    }
}
