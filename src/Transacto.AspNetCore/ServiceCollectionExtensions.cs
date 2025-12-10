using System.Collections;
using System.Collections.Concurrent;
using System.Data.SQLite;
using System.Diagnostics.CodeAnalysis;
using KurrentDB.Client;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using SqlStreamStore;
using Transacto.Framework;
using Transacto.Framework.CommandHandling;
using Transacto.Framework.Http;
using Transacto.Framework.Projections;
using Transacto.Infrastructure.EventStore;
using Transacto.Infrastructure.Npgsql;
using Transacto.Infrastructure.SqlStreamStore;
using Transacto.Plugins;

namespace Transacto;

public static partial class WebApplicationBuilderExtensions {
	extension(WebApplicationBuilder builder) {
		public void AddTransacto(
			KurrentDBClient eventStore,
			NpgsqlConnectionStringBuilder npgsqlConnectionStringBuilder,
			Func<Type, SQLiteConnectionStringBuilder> sqliteConnectionStringBuilderFactory,
			IStreamStore streamStore, params IPlugin[] plugins) {
			var npgsqlConnectionStringBuilderCache = new ConcurrentDictionary<string, NpgsqlConnectionStringBuilder>();

			var messageTypeMapper = MessageTypeMapper.Create(Array.ConvertAll(plugins,
				plugin => new MessageTypeMapper(plugin.MessageTypes)));

			_ = plugins.Concat(Standard.Plugins).Aggregate(builder.Services
					.AddSingleton(eventStore)
					.AddSingleton(npgsqlConnectionStringBuilder)
					.AddSingleton(streamStore)
					.AddHostedService<EventLogging>()
					.AddRouting()
					.AddSingleton(messageTypeMapper)
					// projections
					.AddSingleton<Func<IPlugin, NpgsqlConnection>>(NpqsqlConnectonFactory)
					.AddRazorComponents().Services
					.AddScoped<ViewContext>()
					.AddSingleton(GetRootUri)
					.AddSingleton<IndexRepresentation>(),
				(services, plugin) => {
					var pluginServices = new PluginServiceCollection(NullLogger<PluginServiceCollection>.Instance);

					foreach (var sd in builder.Logging.Services) {
						pluginServices.Add(sd);
					}

					plugin.ConfigureServices(pluginServices);

					var pluginProvider = pluginServices
						.AddSingleton<CommandDispatcher>()
						.AddSingleton(eventStore)
						.AddSingleton(streamStore)
						.AddSingleton(messageTypeMapper)
						.AddSingleton<IPluginDescriptor>(plugin)
						.AddSingleton<InMemoryProjectionDatabase>()
						.AddProjectionHost<InMemoryProjectionHost>()
						.AddProjectionHost<NpgsqlProjectionHost>()
						.AddHostedService<StreamStoreProjectionHost>()
						.AddSingleton<ProjectionsController>()
						.AddSingleton(sqliteConnectionStringBuilderFactory)
						.AddSingleton<Func<NpgsqlConnection>>(() => NpqsqlConnectonFactory(plugin))
						.AddSingleton<IConfiguration>(builder.Configuration)
						.AddSingleton(builder.Environment)
						.AddRazorComponents().Services
						.AddScoped<ViewContext>()
						.AddSingleton(GetPluginRootUri)
						.BuildServiceProvider();

					return services
						.AddKeyedSingleton<IServiceProvider>(plugin, pluginProvider)
						.AddSingleton<IHostedService>(_ => new HostedServicesBackgroundService(plugin, pluginProvider));
				});

			NpgsqlConnection NpqsqlConnectonFactory(IPlugin plugin) =>
				new(npgsqlConnectionStringBuilderCache.GetOrAdd(plugin.Name, static (username, cs) =>
					new NpgsqlConnectionStringBuilder(cs.ConnectionString) {
						Username = username
					}, npgsqlConnectionStringBuilder).ConnectionString);
		}

		private static SiteRoot GetPluginRootUri(IServiceProvider provider) {
			var pluginDescriptor = provider.GetRequiredService<IPluginDescriptor>();
			var uriBuilder = new UriBuilder(GetRootUri(provider).Uri);
			uriBuilder.Path += pluginDescriptor.Slug;
			return new(uriBuilder.Uri);
		}

		private static SiteRoot GetRootUri(IServiceProvider provider) {
			var serverAddresses = provider.GetRequiredService<IServer>()
				.Features.Get<IServerAddressesFeature>();

			if (serverAddresses?.Addresses.Count > 0) {
				return new(new Uri(serverAddresses.Addresses.First()));
			}

			return new(new UriBuilder {
				Port = 5000
			}.Uri);
		}
	}

	private class HostedServicesBackgroundService(IPluginDescriptor plugin, IServiceProvider serviceProvider)
		: IHostedService {
		public string Name => plugin.Name;

		public Task StartAsync(CancellationToken cancellationToken) =>
			Task.WhenAll(serviceProvider.GetServices<IHostedService>()
				.Select(service => service.StartAsync(cancellationToken)));

		public Task StopAsync(CancellationToken cancellationToken) =>
			Task.WhenAll(serviceProvider.GetServices<IHostedService>()
				.Select(service => service.StopAsync(cancellationToken)));
	}

	private partial class PluginServiceCollection(ILogger logger) : IServiceCollection {
		private readonly ServiceCollection _inner = [];

		[ExcludeFromCodeCoverage]
		public IEnumerator<ServiceDescriptor> GetEnumerator() => _inner.GetEnumerator();

		[ExcludeFromCodeCoverage]
		IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_inner).GetEnumerator();

		public void Add(ServiceDescriptor serviceDescriptor) {
			LogAddition(logger, serviceDescriptor);
			_inner.Add(serviceDescriptor);
		}

		[ExcludeFromCodeCoverage]
		public void Clear() {
			LogCleared(logger);
			_inner.Clear();
		}

		[ExcludeFromCodeCoverage]
		public bool Contains(ServiceDescriptor item) => _inner.Contains(item);

		public void CopyTo(ServiceDescriptor[] array, int arrayIndex) => _inner.CopyTo(array, arrayIndex);

		[ExcludeFromCodeCoverage]
		public bool Remove(ServiceDescriptor serviceDescriptor) {
			LogRemoval(logger, serviceDescriptor);
			return _inner.Remove(serviceDescriptor);
		}

		public int Count => _inner.Count;
		[ExcludeFromCodeCoverage] public bool IsReadOnly => _inner.IsReadOnly;

		[ExcludeFromCodeCoverage]
		public int IndexOf(ServiceDescriptor item) => _inner.IndexOf(item);

		[ExcludeFromCodeCoverage]
		public void Insert(int index, ServiceDescriptor item) {
			LogInsertion(logger, item, index);
			_inner.Insert(index, item);
		}

		[ExcludeFromCodeCoverage]
		public void RemoveAt(int index) {
			LogRemovingIndex(logger, index);
			_inner.RemoveAt(index);
		}

		public ServiceDescriptor this[int index] {
			get => _inner[index];
			[ExcludeFromCodeCoverage] set => _inner[index] = value;
		}

		[LoggerMessage(LogLevel.Trace, "Adding {serviceDescriptor}")]
		static partial void LogAddition(ILogger logger, ServiceDescriptor serviceDescriptor);

		[LoggerMessage(LogLevel.Trace, "Cleared")]
		static partial void LogCleared(ILogger logger);

		[LoggerMessage(LogLevel.Trace, "Removing {serviceDescriptor}")]
		static partial void LogRemoval(ILogger logger, ServiceDescriptor serviceDescriptor);

		[LoggerMessage(LogLevel.Trace, "Inserting {serviceDescriptor} at {index}")]
		static partial void LogInsertion(ILogger logger, ServiceDescriptor serviceDescriptor, int index);

		[LoggerMessage(LogLevel.Trace, "Removing {index}")]
		static partial void LogRemovingIndex(ILogger logger, int index);
	}
}
