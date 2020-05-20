using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client;
using Transacto.Domain;
using Transacto.Framework;
using Transacto.Framework.CommandHandling;
using Transacto.Messages;

namespace Transacto.Infrastructure {
	public class GeneralLedgerEventStoreRepository : IGeneralLedgerRepository {
		private readonly EventStoreClient _eventStore;
		private readonly IMessageTypeMapper _messageTypeMapper;
		private readonly UnitOfWork _unitOfWork;
		private readonly EventStoreRepository<GeneralLedger> _inner;

		public GeneralLedgerEventStoreRepository(EventStoreClient eventStore, IMessageTypeMapper messageTypeMapper,
			UnitOfWork unitOfWork) {
			_eventStore = eventStore;
			_messageTypeMapper = messageTypeMapper;
			_unitOfWork = unitOfWork;
			_inner = new EventStoreRepository<GeneralLedger>(eventStore, unitOfWork, GeneralLedger.Factory, messageTypeMapper);
		}

		public async ValueTask<GeneralLedger> Get(CancellationToken cancellationToken = default) {
			if (_unitOfWork.TryGet(GeneralLedger.Identifier, out var a) && a is GeneralLedger generalLedger) {
				return generalLedger;
			}

			await using var events = _eventStore.ReadStreamAsync(Direction.Backwards,
				GeneralLedger.Identifier, StreamPosition.End, int.MaxValue, cancellationToken: cancellationToken);

			generalLedger = GeneralLedger.Factory();

			var stack = new Stack<object>();
			bool lastEventRead = false;
			StreamPosition streamPosition = default;

			await foreach (var resolvedEvent in events) {
				if (!lastEventRead) {
					streamPosition = resolvedEvent.OriginalEvent.EventNumber;
					lastEventRead = true;
				}

				var @event = JsonSerializer.Deserialize(resolvedEvent.OriginalEvent.Data.Span,
					_messageTypeMapper.Map(resolvedEvent.OriginalEvent.EventType),
					TransactoSerializerOptions.Events);
				stack.Push(@event);

				if (@event is GeneralLedgerOpened || @event is AccountingPeriodClosed) {
					break;
				}
			}

			generalLedger.LoadFromHistory(stack);

			_unitOfWork.Attach(GeneralLedger.Identifier, generalLedger, streamPosition.ToInt64());

			return generalLedger;
		}

		public void Add(GeneralLedger generalLedger) => _inner.Add(generalLedger);
	}
}
