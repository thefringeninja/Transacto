using System.Text.Json;
using EventStore.Grpc;
using Transacto.Application;
using Transacto.Framework;
using Transacto.Infrastructure;
using Transacto.Messages;

namespace Transacto.Modules {
    public class AccountingPeriodModule : CommandHandlerModule {
        public AccountingPeriodModule(EventStoreGrpcClient eventStore, JsonSerializerOptions serializerOptions) {
            Build<OpenAccountingPeriod>()
                .Log()
                .UnitOfWork(eventStore, serializerOptions)
                .Handle((_, ct) => {
                    var (unitOfWork, command) = _;
                    var handlers =
                        new AccountingPeriodHandlers(new AccountingPeriodEventStoreRepository(eventStore, unitOfWork));

                    return handlers.Handle(command, ct);
                });

            Build<CloseAccountingPeriod>()
                .Log()
                .UnitOfWork(eventStore, serializerOptions)
                .Handle((_, ct) => {
                    var (unitOfWork, command) = _;
                    var handlers =
                        new AccountingPeriodHandlers(new AccountingPeriodEventStoreRepository(eventStore, unitOfWork));

                    return handlers.Handle(command, ct);
                });
        }
    }
}
