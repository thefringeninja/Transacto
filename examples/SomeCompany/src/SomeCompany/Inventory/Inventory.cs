using EventStore.Client;
using Transacto;
using Transacto.Framework;
using Transacto.Framework.CommandHandling;

namespace SomeCompany.Inventory;

public class Inventory : IPlugin {
	public string Name { get; } = nameof(Inventory);

	public void Configure(IEndpointRouteBuilder builder) => builder
		.MapCommands(string.Empty, typeof(DefineInventoryItem));

	public void ConfigureServices(IServiceCollection services) => services
		.AddSingleton<CommandHandlerModule>(provider => new InventoryItemModule(
			provider.GetRequiredService<EventStoreClient>(),
			provider.GetRequiredService<IMessageTypeMapper>()))
		.AddNpgSqlProjection<InventoryLedgerProjection>();

	public IEnumerable<Type> MessageTypes { get { yield return typeof(InventoryItemDefined); } }
}
