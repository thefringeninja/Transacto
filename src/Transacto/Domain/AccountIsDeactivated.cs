namespace Transacto.Domain;

public delegate bool AccountIsDeactivated(AccountNumber accountNumber);

public static class AccountIsDeactivatedExtensions {
	extension(AccountIsDeactivated) {
		public static bool Ignore(AccountNumber _) => false;
	}
}
