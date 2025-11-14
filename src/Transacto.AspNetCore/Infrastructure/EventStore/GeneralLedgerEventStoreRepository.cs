using System.Text.Json;
using EventStore.Client;
using Transacto.Domain;
using Transacto.Framework;
using Transacto.Messages;

namespace Transacto.Infrastructure.EventStore;

public class GeneralLedgerEventStoreRepository(EventStoreClient eventStore, IMessageTypeMapper messageTypeMapper)
	: IGeneralLedgerRepository {
	private readonly EventStoreRepository<GeneralLedger> _inner = new(eventStore, messageTypeMapper);

	public async ValueTask<GeneralLedger> Get(CancellationToken cancellationToken = default) {
		if (UnitOfWork.Current.TryGet(GeneralLedger.Identifier, out var a) && a is GeneralLedger generalLedger) {
			return generalLedger;
		}

		var events = eventStore.ReadStreamAsync(Direction.Backwards,
			GeneralLedger.Identifier, StreamPosition.End, int.MaxValue, cancellationToken: cancellationToken);

		generalLedger = GeneralLedger.Factory();

		var stack = new Stack<object>();
		bool streamPositionAssigned = false;
		StreamPosition streamPosition = default;

		await foreach (var resolvedEvent in events) {
			if (!streamPositionAssigned) {
				streamPosition = resolvedEvent.OriginalEvent.EventNumber;
				streamPositionAssigned = true;
			}

			var @event = JsonSerializer.Deserialize(resolvedEvent.OriginalEvent.Data.Span,
				messageTypeMapper.Map(resolvedEvent.OriginalEvent.EventType),
				TransactoSerializerOptions.Events)!;
			stack.Push(@event);

			if (@event is GeneralLedgerOpened or AccountingPeriodClosed) {
				break;
			}
		}

		foreach (var e in stack) {
			generalLedger.ReadFromHistory(e);
		}

		UnitOfWork.Current.Attach(new(GeneralLedger.Identifier, generalLedger,
			new Expected.Revision(StreamRevision.FromStreamPosition(streamPosition))));

		return generalLedger;
	}

	public void Add(GeneralLedger generalLedger) => _inner.Add(generalLedger);
}
