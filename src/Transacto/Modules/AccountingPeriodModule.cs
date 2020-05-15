using System.Text.Json;
using EventStore.Client;
using Transacto.Application;
using Transacto.Framework;
using Transacto.Infrastructure;
using Transacto.Messages;
using JsonSerializerOptions = System.Text.Json.JsonSerializerOptions;

namespace Transacto.Modules {
    public class AccountingPeriodModule : CommandHandlerModule {
        public AccountingPeriodModule(EventStoreClient eventStore,
            IMessageTypeMapper messageTypeMapper, JsonSerializerOptions serializerOptions) {
            Build<OpenAccountingPeriod>()
                .Log()
                .UnitOfWork(eventStore, messageTypeMapper, serializerOptions)
                .Handle((_, ct) => {
                    var (unitOfWork, command) = _;
                    var handlers =
                        new AccountingPeriodHandlers(
                            new AccountingPeriodEventStoreRepository(eventStore, messageTypeMapper, unitOfWork));

                    return handlers.Handle(command, ct);
                });

            Build<CloseAccountingPeriod>()
                .Log()
                .UnitOfWork(eventStore, messageTypeMapper, serializerOptions)
                .Handle((_, ct) => {
                    var (unitOfWork, command) = _;
                    var handlers =
                        new AccountingPeriodHandlers(
                            new AccountingPeriodEventStoreRepository(eventStore, messageTypeMapper, unitOfWork));

                    return handlers.Handle(command, ct);
                });
        }
    }
}
