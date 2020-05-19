using System;
using Transacto.Framework;

namespace SomeCompany.Inventory {
	public class InventoryItem : AggregateRoot {
		public static readonly Func<InventoryItem> Factory = () => new InventoryItem();

		public InventoryItemIdentifier Identifier { get; private set; }
		public override string Id => Identifier.ToString();

		private InventoryItem() {
			Register<InventoryItemDefined>(e => Identifier = new InventoryItemIdentifier(e.InventoryItemId));
		}

		public static InventoryItem Define(InventoryItemIdentifier identifier, Sku sku) {
			var inventoryItem = new InventoryItem();

			inventoryItem.Apply(new InventoryItemDefined {
				Sku = sku.ToString(),
				InventoryItemId = identifier.ToGuid()
			});

			return inventoryItem;
		}
	}
}
