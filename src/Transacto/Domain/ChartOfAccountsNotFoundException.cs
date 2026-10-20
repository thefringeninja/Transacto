namespace Transacto.Domain;

public class ChartOfAccountsNotFoundException()
	: Exception("The Chart of Accounts was not found. Make sure it is configured before continuing.");
