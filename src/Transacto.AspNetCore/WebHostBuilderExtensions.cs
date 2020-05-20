using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable CheckNamespace
namespace Microsoft.AspNetCore.Hosting {
// ReSharper restore CheckNamespace

    internal static class WebHostBuilderExtensions {
        public static IWebHostBuilder UseStartup(this IWebHostBuilder builder, IStartup startup) {
	        var startupType = startup.GetType();
	        var startupAssemblyName = startupType.GetTypeInfo().Assembly.GetName().Name;

	        return builder.UseSetting(WebHostDefaults.ApplicationKey, startupAssemblyName)
		        .ConfigureServices(services => services.AddSingleton(startup));
        }
    }
}
