using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

// ReSharper disable once CheckNamespace
namespace System;

internal static class ArgumentExceptionExtensions {
	extension(ArgumentException) {
		public static void ThrowIfLengthTooLong([NotNull] string argument, int maxLength,
			[CallerArgumentExpression(nameof(argument))]
			string? paramName = null) {
			if (argument.Length > maxLength) {
				throw new ArgumentException($"The value cannot be longer than  {maxLength}", paramName);
			}
		}
	}
}
