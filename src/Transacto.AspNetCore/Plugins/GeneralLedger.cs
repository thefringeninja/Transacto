using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Routing;
using Transacto.Domain;
using Transacto.Messages;

namespace Transacto.Plugins {
	internal class GeneralLedger : IPlugin {
		public string Name { get; } = nameof(GeneralLedger);

		public void Configure(IEndpointRouteBuilder builder) => builder
			.MapBusinessTransaction<JournalEntry>("/entries")
			.MapCommands(string.Empty,
				typeof(OpenGeneralLedger),
				typeof(BeginClosingAccountingPeriod))
			;

		public IEnumerable<Type> MessageTypes => Enumerable.Empty<Type>();
	}
}
