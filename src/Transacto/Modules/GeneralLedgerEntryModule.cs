using System.Text.Json;
using EventStore.Grpc;
using Transacto.Application;
using Transacto.Framework;
using Transacto.Infrastructure;
using Transacto.Messages;

namespace Transacto.Modules {
    public class GeneralLedgerEntryModule : CommandHandlerModule {
        public GeneralLedgerEntryModule(EventStoreGrpcClient eventStore, IMessageTypeMapper messageTypeMapper,
            JsonSerializerOptions serializerOptions) {
            Build<PostGeneralLedgerEntry>()
                .Log()
                .UnitOfWork(eventStore, messageTypeMapper, serializerOptions)
                .Handle((_, ct) => {
                    var (unitOfWork, command) = _;
                    var handlers =
                        new GeneralLedgerEntryHandlers(
                            new GeneralLedgerEntryEventStoreRepository(eventStore, messageTypeMapper, unitOfWork));

                    return handlers.Handle(command, ct);
                });
        }
    }
}
