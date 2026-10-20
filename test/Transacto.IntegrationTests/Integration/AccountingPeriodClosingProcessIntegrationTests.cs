using System.Collections.Immutable;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using KurrentDB.Client;
using NodaTime;
using Transacto.Domain;
using Transacto.Framework.Http;
using Transacto.Messages;
using EventTypeFilter = KurrentDB.Client.EventTypeFilter;

namespace Transacto.Integration;

public class AccountingPeriodClosingProcessIntegrationTests(TextWriter textWriter) : IntegrationTests(textWriter) {
	[AutoFixtureData(1)]
	private async Task when_closing_the_period(LocalDate createdOn,
		GeneralLedgerEntryIdentifier closingEntryIdentifier) {
		var period = AccountingPeriod.Open(createdOn);
		await OpenBooks(createdOn).ToArrayAsync();

		var command = new BeginClosingAccountingPeriod {
			ClosingOn = createdOn,
			ClosingGeneralLedgerEntryId = closingEntryIdentifier.ToGuid(),
			RetainedEarningsAccountNumber = 3030,
			GeneralLedgerEntryIds = ImmutableArray<Guid>.Empty
		};

		await HttpClient.SendCommand("/general-ledger", command, TransactoHttpSerializerOptions.Commands);

		var (accountingPeriodClosed, position, checkpoint) = await ReadData();

		Assert.Equal(period, AccountingPeriod.Parse(accountingPeriodClosed.Period));
		Assert.Equal(closingEntryIdentifier,
			new GeneralLedgerEntryIdentifier(accountingPeriodClosed.ClosingGeneralLedgerEntryId));
		Assert.Equal(position, checkpoint);

		return;

		async ValueTask<(AccountingPeriodClosed accountingPeriodClosed, Position accountingPeriodClosedPosition,
			Position checkpointPosition)> ReadData() {
			await using var subscription = KurrentDBClient.SubscribeToAll(FromAll.Start,
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
							TransactoSerializerOptions.Messages)!;
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
