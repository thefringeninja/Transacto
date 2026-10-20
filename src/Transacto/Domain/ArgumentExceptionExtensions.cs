using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Transacto.Domain;

// ReSharper disable once CheckNamespace
namespace System;

internal static class ArgumentExceptionExtensions {
	extension(ArgumentException) {
		public static void ThrowIfLengthTooLong(string argument, int maxLength,
			[CallerArgumentExpression(nameof(argument))]
			string? paramName = null) {
			if (argument.Length > maxLength) {
				throw new ArgumentException($"The value cannot be longer than  {maxLength}", paramName);
			}
		}

		public static void ThrowIfAccountIsNot<TAccount>(AccountNumber accountNumber,
			[CallerArgumentExpression(nameof(accountNumber))]
			string? paramName = null) where TAccount : Account {
			if (Account.For(accountNumber) is not TAccount) {
				throw new ArgumentException($"The account must be a {typeof(TAccount).Name}", paramName);
			}
		}
	}
}
