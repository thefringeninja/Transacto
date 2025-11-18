using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;
using Projac.SQLite;
using Projac.Sql;

namespace Transacto.Infrastructure.SQLite;

public abstract class SQLiteProjection(SQLiteScripts scripts, [CallerFilePath] string name = null!) : SqlProjection {
	protected SQLiteScripts Scripts { get; } = scripts;
	protected static readonly SQLiteSyntax Sql = new();
	public string Name { get; } = Path.GetFileNameWithoutExtension(name);

	protected void When<TEvent>() =>
		When<TEvent>(_ => Sql.NonQueryStatement(Scripts[typeof(TEvent)]));

	protected void When<TEvent>(Func<TEvent, DbParameter[]> handler) =>
		When<TEvent>(e => new SqlNonQueryCommand(Scripts[typeof(TEvent)], handler(e), CommandType.Text));

	protected void When<TEvent>(Func<TEvent, DbParameter[][]> handler) =>
		When<TEvent>(e =>
			Array.ConvertAll(handler(e),
				parameters => new SqlNonQueryCommand(Scripts[typeof(TEvent)], parameters, CommandType.Text)));
}
