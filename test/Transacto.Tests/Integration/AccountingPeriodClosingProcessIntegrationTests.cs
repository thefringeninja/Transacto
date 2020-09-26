using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using EventStore.Client;
using Transacto.Domain;
using Transacto.Messages;
using Xunit;

namespace Transacto.Integration {
	public class AccountingPeriodClosingProcessIntegrationTests : IntegrationTests {
		[Theory, AutoTransactoData(1)]
		public async Task when_closing_the_period(DateTimeOffset createdOn,
			GeneralLedgerEntryIdentifier closingEntryIdentifier) {
			var accountingPeriodClosedSource = new TaskCompletionSource<ResolvedEvent>();
			var checkpointSource = new TaskCompletionSource<Position>();

			await EventStoreClient.SubscribeToAllAsync((s, e, ct) => {
				if (e.Event.EventType == nameof(AccountingPeriodClosed)) {
					accountingPeriodClosedSource.TrySetResult(e);
				}
				else if (e.Event.EventType == "checkpoint") {
					checkpointSource.TrySetResult(
						new Position(
							BitConverter.ToUInt64(e.Event.Data.Span),
							BitConverter.ToUInt64(e.Event.Data.Span.Slice(8))));
				}

				return Task.CompletedTask;
			}, subscriptionDropped: (subscription, reason, ex) => {
				if (ex != null) {
					accountingPeriodClosedSource.TrySetException(ex);
					checkpointSource.TrySetException(ex);
				}
			});

			var period = Period.Open(createdOn);
			await OpenBooks(createdOn).LastAsync();

			var command = new BeginClosingAccountingPeriod {
				ClosingOn = createdOn,
				ClosingGeneralLedgerEntryId = closingEntryIdentifier.ToGuid(),
				RetainedEarningsAccountNumber = 3900
			};

			await HttpClient.SendCommand("/general-ledger", command, TransactoSerializerOptions.Commands);

			var accountingPeriodClosedEvent = await accountingPeriodClosedSource.Task;
			var accountingPeriodClosed = JsonSerializer.Deserialize<AccountingPeriodClosed>(
				Encoding.UTF8.GetString(accountingPeriodClosedEvent.OriginalEvent.Data.Span),
				TransactoSerializerOptions.Events);
			var checkpoint = await checkpointSource.Task;

			Assert.Equal(period, Period.Parse(accountingPeriodClosed.Period));
			Assert.Equal(closingEntryIdentifier,
				new GeneralLedgerEntryIdentifier(accountingPeriodClosed.ClosingGeneralLedgerEntryId));
			Assert.Equal(accountingPeriodClosedEvent.OriginalPosition.Value, checkpoint);
		}
	}
}
