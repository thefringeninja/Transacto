using SomeCompany.Infrastructure;

namespace SomeCompany.PurchaseOrders {
    public class Scripts : NpgsqlScripts {
        public Scripts(string schema) : base(schema) {
        }
    }
}
