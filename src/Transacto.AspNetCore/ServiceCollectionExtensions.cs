using System.Collections.Generic;
using EventStore.Client;
using Microsoft.Extensions.Hosting;
using Projac;
using Transacto;
using Transacto.Domain;
using Transacto.Framework;
using Transacto.Infrastructure;
using Transacto.Messages;
using Transacto.Modules;

// ReSharper disable CheckNamespace
namespace Microsoft.Extensions.DependencyInjection {
	// ReSharper restore CheckNamespace

	public static class ServiceCollectionExtensions {
		public static IServiceCollection AddTransacto(this IServiceCollection services,
			IMessageTypeMapper messageTypeMapper) => services
			.AddRouting()
			.AddSingleton(messageTypeMapper)
			.AddSingleton(new InMemoryReadModel())
			.AddSingleton<CommandHandlerModule>(provider => new GeneralLedgerEntryModule(
				provider.GetRequiredService<EventStoreClient>(),
				provider.GetRequiredService<IMessageTypeMapper>(),
				TransactoSerializerOptions.EventSerializerOptions))
			.AddSingleton<CommandHandlerModule>(provider => new ChartOfAccountsModule(
				provider.GetRequiredService<EventStoreClient>(),
				provider.GetRequiredService<IMessageTypeMapper>(),
				TransactoSerializerOptions.EventSerializerOptions))
			.AddSingleton<CommandHandlerModule>(provider => new AccountingPeriodModule(
				provider.GetRequiredService<EventStoreClient>(),
				provider.GetRequiredService<IMessageTypeMapper>(),
				TransactoSerializerOptions.EventSerializerOptions))
			.AddSingleton<IHostedService>(provider =>
				new InMemoryProjectionHost(
					provider.GetRequiredService<EventStoreClient>(),
					provider.GetRequiredService<IMessageTypeMapper>(),
					provider.GetRequiredService<InMemoryReadModel>(),
					new AnonymousProjectionBuilder<InMemoryReadModel>()
						.When<AccountDefined>((readModel, e) =>
							readModel.Update<Dictionary<int, (string, bool)>>(
								nameof(ChartOfAccounts), rm => rm.Add(e.AccountNumber, (e.AccountName, true))))
						.When<AccountDeactivated>((readModel, e) =>
							readModel.Update<Dictionary<int, (string, bool)>>(
								nameof(ChartOfAccounts),
								rm => rm[e.AccountNumber] = (rm[e.AccountNumber].Item1, false)))
						.When<AccountReactivated>((readModel, e) =>
							readModel.Update<Dictionary<int, (string, bool)>>(
								nameof(ChartOfAccounts),
								rm => rm[e.AccountNumber] = (rm[e.AccountNumber].Item1, true)))
						.When<AccountRenamed>((readModel, e) =>
							readModel.Update<Dictionary<int, (string, bool)>>(
								nameof(ChartOfAccounts),
								rm => rm[e.AccountNumber] = (e.NewAccountName, rm[e.AccountNumber].Item2)))
						.Build()));
	}
}
