using EventStore.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Serilog;
using SqlStreamStore;

namespace Transacto;

public class TransactoHost {
	public static WebApplicationBuilder Create(EventStoreClient eventStore,
		NpgsqlConnectionStringBuilder connectionStringBuilder, IStreamStore streamStore, params IPlugin[] plugins) {
		var builder = WebApplication.CreateBuilder();
		builder.Logging.AddSerilog();

		builder.Services
			.AddSingleton(eventStore)
			.AddSingleton(connectionStringBuilder)
			.AddSingleton(streamStore)
			.AddTransacto(plugins);

		return builder;
	}
}
