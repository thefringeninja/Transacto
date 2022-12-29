using EventStore.Client;
using Npgsql;
using Serilog;
using SqlStreamStore;

namespace Transacto;

public static class TransactoExtensions {
	public static WebApplicationBuilder AddTransacto(this WebApplicationBuilder builder, EventStoreClient eventStore,
		NpgsqlConnectionStringBuilder connectionStringBuilder, IStreamStore streamStore, params IPlugin[] plugins) {
		builder.Logging.AddSerilog();

		builder.Services
			.AddSingleton(eventStore)
			.AddSingleton(connectionStringBuilder)
			.AddSingleton(streamStore)
			.AddTransacto(plugins);

		return builder;
	}
}
