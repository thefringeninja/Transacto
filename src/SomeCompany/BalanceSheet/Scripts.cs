using SomeCompany.Infrastructure;

namespace SomeCompany.BalanceSheet {
    public class Scripts : NpgsqlScripts {
        public Scripts(string schema) : base(schema) {
        }
    }
}
