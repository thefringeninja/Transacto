using System;
using System.Collections.Generic;
using EventStore.Client;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using SqlStreamStore;
using Transaction.AspNetCore;
using Transacto.Framework;

namespace SomeCompany.PurchaseOrders {
	public class PurchaseOrders : IPlugin {
		public string Name { get; } = nameof(PurchaseOrders);

		public void Configure(IEndpointRouteBuilder builder) {
			var streamStore = builder.ServiceProvider.GetRequiredService<IStreamStore>();
			var connectionFactory = builder.ServiceProvider.GetRequiredService<Func<NpgsqlConnection>>();
			builder.UsePurchaseOrders(new PurchaseOrderRepository(streamStore, Name,
				async ct => {
					var connection = connectionFactory();
					await connection.OpenAsync(ct);
					return connection;
				}));
		}

		public void ConfigureServices(IServiceCollection services)
			=> services.AddSingleton<IHostedService>(provider => new NpgSqlProjectionHost(
				provider.GetRequiredService<EventStoreClient>(),
				provider.GetRequiredService<IMessageTypeMapper>(),
				provider.GetRequiredService<Func<NpgsqlConnection>>()));

		public IEnumerable<Type> MessageTypes { get { yield return typeof(PurchaseOrder); } }
	}
}
