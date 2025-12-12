using Npgsql;
using SqlStreamStore;
using Transacto;
using Transacto.Framework;
using Transacto.Infrastructure.SqlStreamStore;

namespace SomeCompany.PurchaseOrders;

public class PurchaseOrders : IPlugin {
	public string Name { get; } = nameof(PurchaseOrders);

	public void Configure(IEndpointRouteBuilder builder) {
		builder.UsePurchaseOrders();
	}
	public void ConfigureServices(IServiceCollection services) => services
		.AddSingleton<PurchaseOrderRepository>(provider => {
			var connectionFactory = provider.GetRequiredService<Func<NpgsqlConnection>>();
			return new PurchaseOrderRepository(
				provider.GetRequiredService<IStreamStore>(),
				Name,
				async ct => {
					var connection = connectionFactory();
					await connection.OpenAsync(ct);
					return connection;
				});
		})
		.AddNpgSqlProjection<PurchaseOrderListProjection>()
		.AddSingleton<StreamStoreProjection>(provider =>
			new PurchaseOrderFeed(provider.GetRequiredService<IMessageTypeMapper>()));

	public IEnumerable<Type> MessageTypes { get { yield return typeof(PurchaseOrderPlaced); } }
	public IEnumerable<LinkRelation> LinkRelations => [];
}
