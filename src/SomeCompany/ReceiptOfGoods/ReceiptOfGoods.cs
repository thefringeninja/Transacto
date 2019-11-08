using System;
using System.Collections.Generic;
using System.Linq;
using Transacto.Domain;

namespace SomeCompany.ReceiptOfGoods {
    partial class ReceiptOfGoods : IBusinessTransaction {
        public GeneralLedgerEntry GetGeneralLedgerEntry(PeriodIdentifier period, DateTimeOffset createdOn) {
            var entry = GeneralLedgerEntry.Create(
                new GeneralLedgerEntryIdentifier(Guid.Parse((string)ReceiptOfGoodsId)),
                new GeneralLedgerEntryNumber($"goodsreceipt-{ReceiptOfGoodsNumber}"), period, createdOn);

            var (inventoryInTransit, inventoryOnHand) = ReceiptOfGoodsItems.Aggregate(
                (new Credit(new AccountNumber(1400)), new Debit(new AccountNumber(1450))),
                Accumulate);

            entry.ApplyCredit(inventoryInTransit);
            entry.ApplyDebit(inventoryOnHand);
            entry.ApplyTransaction(this);

            return entry;
        }

        private static (Credit, Debit) Accumulate((Credit, Debit) _, ReceiptOfGoodsItems item) {
            var (inventoryInTransit, inventoryOnHand) = _;
            return (inventoryInTransit + item.Total, inventoryOnHand + item.Total);
        }

        public IEnumerable<object> GetAdditionalChanges()
        {
            yield return new GoodsReceived
            {
                ReceiptId = Guid.Parse(ReceiptOfGoodsId),
                ReceiptNumber = ReceiptOfGoodsNumber,
                Items = ReceiptOfGoodsItems.Select(item => new GoodsReceived.ReceiptItem
                {
                    Quantity = Convert.ToDecimal(item.Quantity),
                    InventoryItemId = Guid.Parse(item.ItemId),
                    UnitPrice = Convert.ToDecimal(item.UnitPrice)
                }).ToArray()
            };
        }
    }
}
