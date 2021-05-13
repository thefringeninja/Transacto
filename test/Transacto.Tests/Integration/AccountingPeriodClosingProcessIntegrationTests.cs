using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using EventStore.Client;
using NodaTime;
using Transacto.Domain;
using Transacto.Messages;
using Xunit;

namespace Transacto.Integration {
	public class AccountingPeriodClosingProcessIntegrationTests : IntegrationTests {
		[Theory, AutoTransactoData(1)]
		public async Task when_closing_the_period(LocalDateTime createdOn,
			GeneralLedgerEntryIdentifier closingEntryIdentifier) {
			var accountingPeriodClosedSource = new TaskCompletionSource<ResolvedEvent>();
			var checkpointSource = new TaskCompletionSource<Position>();

			await EventStoreClient.SubscribeToAllAsync((_, e, _) => {
				switch (e.Event.EventType) {
					case nameof(AccountingPeriodClosed):
						accountingPeriodClosedSource.TrySetResult(e);
						break;
					case "checkpoint":
						checkpointSource.TrySetResult(
							new Position(
								BitConverter.ToUInt64(e.Event.Data.Span),
								BitConverter.ToUInt64(e.Event.Data.Span[8..])));
						break;
				}

				return Task.CompletedTask;
			}, subscriptionDropped: (_, _, ex) => {
				if (ex != null) {
					accountingPeriodClosedSource.TrySetException(ex);
					checkpointSource.TrySetException(ex);
				}
			});

			var period = AccountingPeriod.Open(createdOn.Date);
			await OpenBooks(createdOn).LastAsync();

			var command = new BeginClosingAccountingPeriod {
				ClosingOn = createdOn.ToDateTimeUnspecified(),
				ClosingGeneralLedgerEntryId = closingEntryIdentifier.ToGuid(),
				RetainedEarningsAccountNumber = 3900
			};

			await HttpClient.SendCommand("/general-ledger", command, TransactoSerializerOptions.Commands);

			var accountingPeriodClosedEvent = await accountingPeriodClosedSource.Task;
			var accountingPeriodClosed = JsonSerializer.Deserialize<AccountingPeriodClosed>(
				Encoding.UTF8.GetString(accountingPeriodClosedEvent.OriginalEvent.Data.Span),
				TransactoSerializerOptions.Events)!;
			var checkpoint = await checkpointSource.Task;

			Assert.Equal(period, AccountingPeriod.Parse(accountingPeriodClosed.Period));
			Assert.Equal(closingEntryIdentifier,
				new GeneralLedgerEntryIdentifier(accountingPeriodClosed.ClosingGeneralLedgerEntryId));
			Assert.Equal(accountingPeriodClosedEvent.OriginalPosition!.Value, checkpoint);
		}
	}
}
