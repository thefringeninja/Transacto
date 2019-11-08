using SomeCompany.Infrastructure;

namespace SomeCompany.Inventory {
    public class Scripts : NpgsqlScripts {
        public Scripts(string schema) : base(schema) {

        }
    }
}
