using System.Data.Common;
using System.Linq.Expressions;
using Inflector;

// ReSharper disable CheckNamespace
namespace Projac.SQLite;
// ReSharper restore CheckNamespace

public static class SyntaxExtensions {
	public static DbParameter Parameter(this SQLiteSyntax syntax, Expression<Func<object>> exp) {
		var value = exp.Compile().Invoke() switch {
			Guid g => syntax.Guid(g),
			string s => syntax.AnsiString(s),
			decimal d => syntax.Decimal(d),
			int i => syntax.Int32(i),
			long l => syntax.Int64(l),
			DateTime dt => syntax.DateTime(dt),
			_ => throw new NotSupportedException()
		};
		return value.ToDbParameter(exp.Parameters[0].Name.Underscore());
	}
}
