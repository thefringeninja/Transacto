using System;
using System.Linq;

namespace SomeCompany {
    public abstract class Table {
        public string Schema { get; }
        public string TableName { get; }
        public object[] Rows { get; }
        public string[] ColumnNames { get; }
        public Type Type { get; }

        protected Table(string schema, string tableName, Type type, object[] rows) {
            if (schema == null) throw new ArgumentNullException(nameof(schema));
            if (tableName == null) throw new ArgumentNullException(nameof(tableName));
            if (rows == null) throw new ArgumentNullException(nameof(rows));

            Type = type;
            ColumnNames = Array.ConvertAll(Type.GetProperties(), pi => pi.Name);
            Schema = schema;
            TableName = tableName;
            Rows = rows;
        }
    }

    public class Table<T> : Table where T: class {
        public Table(string schema, string tableName, params T[] rows)
            : base(schema, tableName, typeof(T), rows) {

        }
    }
}
