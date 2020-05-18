using System;
using System.Collections.Generic;
using EventStore.Client;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Transacto;
using Transacto.Framework;

namespace SomeCompany.Inventory {
	public class Inventory : IPlugin {
		public string Name { get; } = nameof(Inventory);

		public void Configure(IEndpointRouteBuilder builder)
			=> builder.UseInventory();

		public void ConfigureServices(IServiceCollection services)
			=> services.AddNpgSqlProjection<InventoryLedger>();

		public IEnumerable<Type> MessageTypes { get { yield return typeof(InventoryItemDefined); } }
	}
}
