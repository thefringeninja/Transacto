using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Transacto;

namespace SomeCompany.BalanceSheet {
	public class BalanceSheet : IPlugin {
		public string Name { get; } = nameof(BalanceSheet);

		public void Configure(IEndpointRouteBuilder builder) => builder.UseBalanceSheet();

		public void ConfigureServices(IServiceCollection services)
			=> services.AddNpgSqlProjection<BalanceSheetReportProjection>();

		public IEnumerable<Type> MessageTypes => Array.Empty<Type>();
	}
}
