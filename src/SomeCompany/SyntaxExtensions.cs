using System;
using System.Data.Common;
using System.Linq.Expressions;
using Inflector;

// ReSharper disable CheckNamespace
namespace Projac.Npgsql {
	// ReSharper restore CheckNamespace

	internal static class SyntaxExtensions {
		public static DbParameter GetParameter(this NpgsqlSyntax syntax, Expression<Func<object>> exp) {
			var value = exp.Compile().Invoke() switch {
				Guid g => syntax.UniqueIdentifier(g),
				string s => syntax.NVarCharMax(s),
				decimal d => syntax.Money(d),
				int i => syntax.Int(i),
				long l => syntax.BigInt(l),
				_ => throw new NotSupportedException()
			};
			return value.ToDbParameter(exp.Parameters[0].Name.Underscore());
		}
	}
}
