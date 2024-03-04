using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using EventStore.Client;
using NodaTime;
using Transacto.Domain;
using Transacto.Messages;
using EventTypeFilter = EventStore.Client.EventTypeFilter;

namespace Transacto.Integration;

public class AccountingPeriodClosingProcessIntegrationTests : IntegrationTests {
	[AutoFixtureData(1)]
	public async Task when_closing_the_period(LocalDateTime createdOn,
		GeneralLedgerEntryIdentifier closingEntryIdentifier) {
		var period = AccountingPeriod.Open(createdOn.Date);
		await OpenBooks(createdOn).LastAsync();

		var command = new BeginClosingAccountingPeriod {
			ClosingOn = createdOn.ToDateTimeUnspecified(),
			ClosingGeneralLedgerEntryId = closingEntryIdentifier.ToGuid(),
			RetainedEarningsAccountNumber = 3900,
			GeneralLedgerEntryIds = ImmutableArray<Guid>.Empty
		};

		await HttpClient.SendCommand("/general-ledger", command, TransactoSerializerOptions.Commands);

		var (accountingPeriodClosed, position, checkpoint) = await ReadData();

		Assert.Equal(period, AccountingPeriod.Parse(accountingPeriodClosed.Period));
		Assert.Equal(closingEntryIdentifier,
			new GeneralLedgerEntryIdentifier(accountingPeriodClosed.ClosingGeneralLedgerEntryId));
		Assert.Equal(position, checkpoint);

		return;

		async ValueTask<(AccountingPeriodClosed accountingPeriodClosed, Position accountingPeriodClosedPosition,
			Position checkpointPosition)> ReadData() {
			await using var subscription = EventStoreClient.SubscribeToAll(FromAll.Start,
				filterOptions: new SubscriptionFilterOptions(EventTypeFilter.ExcludeSystemEvents()));

			AccountingPeriodClosed? accountingPeriodClosed = null;
			Position? position = null;
			Position? checkpoint = null;
			
			await foreach (var message in subscription.Messages) {
				if (message is not StreamMessage.Event(var resolvedEvent)) {
					continue;
				}

				switch (resolvedEvent.OriginalEvent.EventType) {
					case "checkpoint":
						checkpoint = new Position(
							BitConverter.ToUInt64(resolvedEvent.Event.Data.Span),
							BitConverter.ToUInt64(resolvedEvent.Event.Data.Span));
						break;
					case nameof(AccountingPeriodClosed):
						accountingPeriodClosed = JsonSerializer.Deserialize<AccountingPeriodClosed>(
							Encoding.UTF8.GetString(resolvedEvent.OriginalEvent.Data.Span),
							TransactoSerializerOptions.Events)!;
						position = resolvedEvent.OriginalPosition!.Value;
						break;
				}

				if (position.HasValue && checkpoint.HasValue && accountingPeriodClosed is not null) {
					return (accountingPeriodClosed, position.Value, checkpoint.Value);
				}
			}

			throw new UnreachableException();
		}
	}
}
