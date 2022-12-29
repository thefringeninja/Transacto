using Npgsql;
using SqlStreamStore;
using Transacto;
using Transacto.Framework;
using Transacto.Infrastructure.SqlStreamStore;

namespace SomeCompany.PurchaseOrders;

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

	public void ConfigureServices(IServiceCollection services) => services
		.AddNpgSqlProjection<PurchaseOrderListProjection>()
		.AddSingleton<StreamStoreProjection>(provider =>
			new PurchaseOrderFeed(provider.GetRequiredService<IMessageTypeMapper>()));

	public IEnumerable<Type> MessageTypes { get { yield return typeof(PurchaseOrderPlaced); } }
}
