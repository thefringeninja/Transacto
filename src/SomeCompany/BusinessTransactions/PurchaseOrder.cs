using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Messages;
using Transacto.Domain;

namespace SomeCompany.BusinessTransactions {
    public partial class PurchaseOrder : IBusinessTransaction {
        public GeneralLedgerEntry GetGeneralLedgerEntry() {
            var entry = GeneralLedgerEntry.Create(
                new GeneralLedgerEntryIdentifier(Guid.Parse(PurchaseOrderId)),
                new GeneralLedgerEntryNumber($"purchaseorder-{PurchaseOrderNumber}"), DateTimeOffset.Now);

            var (accountsPayable, inventory) = Items.Aggregate(
                (new Credit(new AccountNumber(2150), Money.Zero), new Debit(new AccountNumber(1400), Money.Zero)),
                (x, items) => (x.Item1 + new Money(Convert.ToDecimal(items.Quantity * items.UnitPrice)),
                    x.Item2 + new Money(Convert.ToDecimal(items.Quantity * items.UnitPrice))));

            entry.ApplyCredit(accountsPayable);
            entry.ApplyDebit(inventory);
            entry.ApplyTransaction(this);

            return entry;
        }

        public IEnumerable<object> Transaction {
            get {
                yield return new PurchaseOrderPlaced {
                    PurchaseOrderId = Guid.Parse(PurchaseOrderId),
                    PurchaseOrderNumber = PurchaseOrderNumber,
                    Items = Items.Select(item => new PurchaseOrderPlaced.PurchaseOrderItem {
                        Quantity = item.Quantity,
                        UnitPrice = Convert.ToDecimal(item.UnitPrice),
                        InventoryItemId = Guid.Parse(item.ItemId)
                    }).ToArray()
                };
            }
        }
    }
}
