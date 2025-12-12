using Inflector;
using Transacto.Domain;
using Transacto.Framework.Projections;
using Transacto.Infrastructure.SQLite;
using Transacto.Messages;

namespace Transacto.Plugins.GeneralLedger.LedgerEntryIndex;

public class LedgerEntryIndexProjection : SQLiteProjection {
	public LedgerEntryIndexProjection() : base(new Scripts()) {
		When<CreateSchema>();
		When<GeneralLedgerEntryCreated>(e => {
			var createdOn = Time.Parse.LocalDateTime(e.CreatedOn).ToDateTimeUnspecified();
			return [
				Sql.Guid(e.GeneralLedgerEntryId).ToDbParameter(nameof(e.GeneralLedgerEntryId).Underscore()),

				Sql.DateTime(createdOn).ToDbParameter(nameof(createdOn).Underscore())
			];
		});
	}
}

public class Scripts : SQLiteScripts {
}
