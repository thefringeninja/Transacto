using System;
using System.Linq;
using System.Threading.Tasks;
using KellermanSoftware.CompareNetObjects;
using SqlStreamStore;
using Transacto.Domain;
using Transacto.Messages;
using Xunit;

namespace SomeCompany.PurchaseOrders {
    public class PurchaseOrderTests {
        [Theory, AutoSomeCompanyData]
        public async Task when_persisting_to_storage(PurchaseOrder purchaseOrder) {
            using var streamStore = new InMemoryStreamStore();
            var repository = new PurchaseOrderRepository(streamStore);

            await repository.Save(purchaseOrder);

            var copy = await repository.Get(Guid.Parse(purchaseOrder.PurchaseOrderId));

            Assert.Equal(purchaseOrder, copy, new LogicEqualityComparer<PurchaseOrder> {
                UseObjectHashes = false
            });
        }

        [Theory, AutoSomeCompanyData]
        public void when_getting_the_general_ledger_entry(PurchaseOrder purchaseOrder, DateTimeOffset now) {
            var generalLedgerEntry =
                purchaseOrder.GetGeneralLedgerEntry(new PeriodIdentifier(now.Month, now.Year), now);

            Assert.True(generalLedgerEntry.HasChanges);
            Assert.True(generalLedgerEntry.IsInBalance);
            var generalLedgerEntryCreated =
                Assert.Single(generalLedgerEntry.GetChanges().OfType<GeneralLedgerEntryCreated>());
            Assert.Equal(new GeneralLedgerEntryNumber($"purchaseorder-{purchaseOrder.PurchaseOrderNumber}"),
                new GeneralLedgerEntryNumber(generalLedgerEntryCreated.Number));

            var debits = generalLedgerEntry.GetChanges().OfType<DebitApplied>().ToArray();
            var credits = generalLedgerEntry.GetChanges().OfType<CreditApplied>().ToArray();

            var debit = Assert.Single(debits);
            var credit = Assert.Single(credits);

            Assert.Equal(new AccountNumber(1400), new AccountNumber(debit.AccountNumber));
            Assert.Equal(new AccountNumber(2150), new AccountNumber(credit.AccountNumber));

            Assert.Equal(
                new Money(debits.Sum(x => x.Amount)),
                new Money(purchaseOrder.PurchaseOrderItems.Sum(x => x.Total)));
            Assert.Equal(
                new Money(credits.Sum(x => x.Amount)),
                new Money(purchaseOrder.PurchaseOrderItems.Sum(x => x.Total)));
        }
    }
}
