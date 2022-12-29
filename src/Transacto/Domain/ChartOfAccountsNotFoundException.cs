namespace Transacto.Domain;

public class ChartOfAccountsNotFoundException : Exception {
	public ChartOfAccountsNotFoundException() : base("The Chart of Accounts was not found.") {
	}
}
