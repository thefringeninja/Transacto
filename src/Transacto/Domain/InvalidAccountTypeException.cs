using System;

namespace Transacto.Domain {
	public class InvalidAccountTypeException : Exception {
		public AccountType Expected { get; }
		public AccountType Actual { get; }

		public InvalidAccountTypeException(AccountType expected, AccountType actual) : base(
			$"Expected an account type of '{expected.Name}', received '{actual.Name}'.") {
			Expected = expected;
			Actual = actual;
		}
	}
}
