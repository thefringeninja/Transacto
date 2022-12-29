using System.Collections.Immutable;
using NodaTime;
using Transacto.Domain;
using Transacto.Framework;
using Transacto.Infrastructure.SqlStreamStore;
using Transacto.Messages;

namespace Transacto.Integration;

public class BusinessTransactionIntegrationTests : IntegrationTests {
	public async Task Somewthing() {
		var now = DateTime.UtcNow;
		var period = AccountingPeriod.Open(LocalDate.FromDateTime(now));
		var transactionId = Guid.NewGuid();
		await HttpClient.SendCommand("/transactions", new PostGeneralLedgerEntry {
			BusinessTransaction = new BusinessTransaction {
				TransactionId = transactionId,
				TransactionNumber = 1
			},
			Period = period.ToString(),
			CreatedOn = now,
			GeneralLedgerEntryId = transactionId
		}, TransactoSerializerOptions.BusinessTransactions(typeof(BusinessTransaction)));
	}

	private record BusinessTransactionEntry : IFeedEntry {
		public Guid TransactonId { get; init; }
		public int ReferenceNumber { get; init; }

		public required ImmutableArray<string> Events { get; init; }
	}

	private class BusinessTransactionFeed : StreamStoreFeedProjection<BusinessTransactionEntry> {
		public BusinessTransactionFeed(IMessageTypeMapper messageTypeMapper) : base("businessTransactions",
			messageTypeMapper) {
			When<BusinessTransaction>((e, _) => new BusinessTransactionEntry {
				TransactonId = e.TransactionId,
				ReferenceNumber = e.TransactionNumber,
				Events = ImmutableArray<string>.Empty
			});
		}
	}
}
