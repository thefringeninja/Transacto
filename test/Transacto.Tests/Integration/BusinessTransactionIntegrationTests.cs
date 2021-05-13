using System;
using System.Threading.Tasks;
using NodaTime;
using Transacto.Framework;
using Transacto.Framework.Projections.SqlStreamStore;
using Transacto.Messages;
using Xunit;
using Period = Transacto.Domain.Period;

namespace Transacto.Integration {
	public class BusinessTransactionIntegrationTests : IntegrationTests {
		[Fact]
		public async Task Somewthing() {
			var now = DateTime.UtcNow;
			var period = Period.Open(LocalDate.FromDateTime(now));
			var transactionId = Guid.NewGuid();
			await HttpClient.SendCommand("/transactions", new PostGeneralLedgerEntry {
				BusinessTransaction = new BusinessTransaction {
					TransactionId = transactionId,
					ReferenceNumber = 1,
					Version = 1
				},
				Period = period.ToString(),
				CreatedOn = now,
				GeneralLedgerEntryId = transactionId
			}, TransactoSerializerOptions.BusinessTransactions(typeof(BusinessTransaction)));
		}

		private class BusinessTransactionEntry : FeedEntry {
			public Guid TransactonId { get; set; }
			public int ReferenceNumber { get; set; }
		}

		private class BusinessTransactionFeed : StreamStoreFeedProjection<BusinessTransactionEntry> {
			public BusinessTransactionFeed(IMessageTypeMapper messageTypeMapper) : base("businessTransactions",
				messageTypeMapper) {
				When<BusinessTransaction>((e, _) => new BusinessTransactionEntry {
					TransactonId = e.TransactionId,
					ReferenceNumber = e.ReferenceNumber
				});
			}
		}
	}
}
