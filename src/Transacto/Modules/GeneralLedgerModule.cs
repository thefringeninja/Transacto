using System.Text.Json;
using EventStore.Client;
using Transacto.Application;
using Transacto.Framework;
using Transacto.Infrastructure;
using Transacto.Messages;
using JsonSerializerOptions = System.Text.Json.JsonSerializerOptions;

namespace Transacto.Modules {
    public class GeneralLedgerModule : CommandHandlerModule {
        public GeneralLedgerModule(EventStoreClient eventStore,
            IMessageTypeMapper messageTypeMapper, JsonSerializerOptions serializerOptions) {
	        Build<OpenGeneralLedger>()
		        .Log()
		        .UnitOfWork(eventStore, messageTypeMapper, serializerOptions)
		        .Handle((_, ct) => {
			        var (unitOfWork, command) = _;
			        var handlers =
				        new GeneralLedgerHandlers(
					        new GeneralLedgerEventStoreRepository(eventStore, messageTypeMapper, unitOfWork));

			        return handlers.Handle(command, ct);
		        });
            Build<BeginClosingAccountingPeriod>()
                .Log()
                .UnitOfWork(eventStore, messageTypeMapper, serializerOptions)
                .Handle((_, ct) => {
                    var (unitOfWork, command) = _;
                    var handlers =
                        new GeneralLedgerHandlers(
                            new GeneralLedgerEventStoreRepository(eventStore, messageTypeMapper, unitOfWork));

                    return handlers.Handle(command, ct);
                });
        }
    }
}
