using System.Collections;
using System.Collections.Concurrent;
using System.Data.SQLite;
using System.Diagnostics.CodeAnalysis;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Grpc.Core.Interceptors;
using KurrentDB.Client;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.WebEncoders;
using Npgsql;
using SqlStreamStore;
using Transacto.Domain;
using Transacto.Framework;
using Transacto.Framework.CommandHandling;
using Transacto.Framework.Http.Html;
using Transacto.Framework.Logging;
using Transacto.Framework.Projections;
using Transacto.Infrastructure.EventStore;
using Transacto.Infrastructure.Npgsql;
using Transacto.Infrastructure.SqlStreamStore;
using Transacto.Plugins;
using Transacto.Plugins.GeneralLedger;

namespace Transacto;

public static partial class WebApplicationBuilderExtensions {
	public static void AddTransacto(this IServiceCollection serviceCollection,
		KurrentDBClientSettings eventStoreClientSettings,
		NpgsqlConnectionStringBuilder npgsqlConnectionStringBuilder,
		Func<Type, SQLiteConnectionStringBuilder> sqliteConnectionStringBuilderFactory,
		IStreamStore streamStore, params IPlugin[] plugins) {
		var sharedServices = new ServiceCollection();

		foreach (var descriptor in serviceCollection) {
			sharedServices.Add(descriptor);
		}

		var sharedDependencies = sharedServices
			.AddSingleton<Interceptor, DebuggerDeadlineInterceptor>()
			.AddSingleton<KurrentDBClient>(provider => {
				eventStoreClientSettings.LoggerFactory ??= provider.GetRequiredService<ILoggerFactory>();
				eventStoreClientSettings.Interceptors ??= provider.GetServices<Interceptor>();

				return new KurrentDBClient(eventStoreClientSettings);
			})
			.BuildServiceProvider();

		var eventStore = sharedDependencies.GetRequiredService<KurrentDBClient>();

		var allPlugins = plugins.Concat(Standard.Plugins).ToArray();

		var linkRelations = new LinkRelationCollection {
			new LinkRelation("self", "You are here:"),
			new LinkRelation("index", "Navigation")
		};
		foreach (var plugin in allPlugins) {
			foreach (var linkRelation in plugin.LinkRelations) {
				linkRelations.Add(linkRelation);
			}
		}

		var npgsqlConnectionStringBuilderCache = new ConcurrentDictionary<string, NpgsqlConnectionStringBuilder>();

		var messageTypeMapper = MessageTypeMapper.Create(Array.ConvertAll(plugins,
			plugin => new MessageTypeMapper(plugin.MessageTypes)));

		serviceCollection
			.AddSingleton(eventStore)
			.AddSingleton(npgsqlConnectionStringBuilder)
			.AddSingleton(streamStore)
			.AddSingleton(messageTypeMapper)
			.AddSingleton<ISystemClock, SystemClock>()
			.AddHostedService<EventLogging>()
			.AddRouting()
			.AddSingleton<Func<IPlugin, NpgsqlConnection>>(NpgsqlConnectionFactory)
			.AddSingleton<ISettingsRepository, EventStoreSettingsRepository>()
			.AddRazorComponents().Services
			.AddScoped<ViewContext>()
			.AddSingleton(linkRelations)
			.AddSingleton(GetRootUri)
			.AddSingleton<IndexRepresentation>()
			.AddSingleton<GetCurrentPeriodAsync>(provider =>
				provider.GetRequiredKeyedService<IServiceProvider>(Standard.GeneralLedger)
					.GetRequiredService<GetCurrentPeriodAsync>());

		_ = allPlugins.Aggregate(serviceCollection,
			(services, plugin) => {
				var loggerFactory = sharedDependencies.GetRequiredService<ILoggerFactory>();
				var pluginServices =
					new PluginServiceCollection(plugin, loggerFactory.CreateLogger<PluginServiceCollection>());

				pluginServices
					.AddSingleton<Func<Type, IGeneralLedgerEntrySequenceNumberGenerator>>(provider => {
						ConcurrentDictionary<Type, IGeneralLedgerEntrySequenceNumberGenerator> cache = [];

						return type => cache.GetOrAdd(type,
							static (type, provider) => new EventStoreSequenceNumberGenerator(
								provider.GetRequiredService<KurrentDBClient>(),
								provider.GetRequiredService<IMessageTypeMapper>(),
								type,
								provider.GetRequiredService<ILoggerFactory>()
									.CreateLogger<EventStoreSequenceNumberGenerator>()), provider);
					});


				plugin.ConfigureServices(pluginServices);

				pluginServices
					.AddSingleton(loggerFactory)
					.AddSingleton<CommandDispatcher>()
					.AddSingleton(eventStore)
					.AddSingleton(streamStore)
					.AddSingleton(messageTypeMapper)
					.AddSingleton<IPluginDescriptor>(plugin)
					.AddSingleton<ISystemClock, SystemClock>()
					.AddSingleton<InMemoryProjectionDatabase>()
					.AddProjectionHost<InMemoryProjectionHost>()
					.AddProjectionHost<NpgsqlProjectionHost>()
					.AddProjectionHost<StreamStoreProjectionHost>()
					.AddSingleton<ProjectionsController>()
					.AddSingleton(sqliteConnectionStringBuilderFactory)
					.AddSingleton<Func<NpgsqlConnection>>(() => NpgsqlConnectionFactory(plugin))
					.AddSingleton(sharedDependencies.GetRequiredService<IConfiguration>())
					.AddSingleton(sharedDependencies.GetRequiredService<IWebHostEnvironment>())
					.AddSingleton(linkRelations)
					.Configure<WebEncoderOptions>(options =>
						options.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All))
					.AddRazorComponents().Services
					.AddScoped<ViewContext>()
					.AddSingleton(GetPluginRootUri)
					.AddSingleton<GetFormatter>(provider => () => provider
						.GetRequiredService<InMemoryProjectionDatabase>()
						.Get<FormatSettingsReadModel>() switch {
						{ HasValue: true } readModel => readModel.Value.Formatter,
						_ => Formatter.Default
					})
					.AddScoped<Formatter>(provider => provider.GetRequiredService<GetFormatter>()())
					.AddInMemoryProjection<FormatSettingsReadModel>((readModel, e) => e switch {
						Formatter settings => readModel with { Formatter = settings },
						_ => readModel
					});

				var pluginProvider = pluginServices.BuildServiceProvider();

				return services
						.AddKeyedSingleton<IServiceProvider>(plugin, pluginProvider)
						.AddSingleton<IHostedService>(new HostedServicesBackgroundService(plugin, pluginProvider))
					;
			});

		NpgsqlConnection NpgsqlConnectionFactory(IPlugin plugin) =>
			new(npgsqlConnectionStringBuilderCache.GetOrAdd(plugin.Name, static (pluginName, cs) =>
				new NpgsqlConnectionStringBuilder(cs.ConnectionString) {
				}, npgsqlConnectionStringBuilder).ConnectionString);
	}

	extension(WebApplicationBuilder) {
		private static SiteRoot GetPluginRootUri(IServiceProvider provider) {
			var pluginDescriptor = provider.GetRequiredService<IPluginDescriptor>();
			var uriBuilder = new UriBuilder(GetRootUri(provider).Uri);
			uriBuilder.Path += pluginDescriptor.Slug;
			return new(uriBuilder.Uri);
		}

		private static SiteRoot GetRootUri(IServiceProvider provider) =>
			new(new UriBuilder {
				Port = 5000
			}.Uri);
	}

	private record FormatSettingsReadModel : MemoryReadModel {
		public Formatter Formatter { get; init; } = Formatter.Default;
	}

	private class HostedServicesBackgroundService(IPluginDescriptor plugin, IServiceProvider serviceProvider)
		: IHostedService {
		public string Name { get; } = plugin.Name;

		private readonly IHostedService[] _hostedServices = serviceProvider.GetServices<IHostedService>().ToArray();

		public async Task StartAsync(CancellationToken cancellationToken) =>
			await Task.WhenAll(Array.ConvertAll(_hostedServices, (service => service.StartAsync(cancellationToken))));

		public async Task StopAsync(CancellationToken cancellationToken) =>
			await Task.WhenAll(Array.ConvertAll(_hostedServices, (service => service.StopAsync(cancellationToken))));
	}

	private partial class PluginServiceCollection(IPluginDescriptor plugin, ILogger<PluginServiceCollection> logger)
		: IServiceCollection {
		private readonly ServiceCollection _inner = [];
		private readonly Scope _scope = new(plugin.Name);

		[ExcludeFromCodeCoverage]
		public IEnumerator<ServiceDescriptor> GetEnumerator() => _inner.GetEnumerator();

		[ExcludeFromCodeCoverage]
		IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_inner).GetEnumerator();

		public void Add(ServiceDescriptor serviceDescriptor) {
			using (logger.BeginScope(_scope))
				LogAddition(logger, serviceDescriptor);
			_inner.Add(serviceDescriptor);
		}

		[ExcludeFromCodeCoverage]
		public void Clear() {
			using (logger.BeginScope(_scope))
				LogCleared(logger);
			_inner.Clear();
		}

		[ExcludeFromCodeCoverage]
		public bool Contains(ServiceDescriptor item) => _inner.Contains(item);

		public void CopyTo(ServiceDescriptor[] array, int arrayIndex) => _inner.CopyTo(array, arrayIndex);

		[ExcludeFromCodeCoverage]
		public bool Remove(ServiceDescriptor serviceDescriptor) {
			using (logger.BeginScope(_scope))
				LogRemoval(logger, serviceDescriptor);
			return _inner.Remove(serviceDescriptor);
		}

		public int Count => _inner.Count;
		[ExcludeFromCodeCoverage] public bool IsReadOnly => _inner.IsReadOnly;

		[ExcludeFromCodeCoverage]
		public int IndexOf(ServiceDescriptor item) => _inner.IndexOf(item);

		[ExcludeFromCodeCoverage]
		public void Insert(int index, ServiceDescriptor item) {
			using (logger.BeginScope(_scope))
				LogInsertion(logger, item, index);
			_inner.Insert(index, item);
		}

		[ExcludeFromCodeCoverage]
		public void RemoveAt(int index) {
			using (logger.BeginScope(_scope))
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

		[LoggingScope]
		private partial record Scope(string Plugin);
	}
}
