using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Serilog.Context;
using Transacto;
using Transacto.Domain;
using Transacto.Framework;
using Transacto.Framework.Http;
using Transacto.Framework.Projections;
using Transacto.Plugins;
using Index = Transacto.Plugins.Index;

// ReSharper disable CheckNamespace
namespace Microsoft.AspNetCore.Builder;
// ReSharper restore CheckNamespace

static partial class ApplicationBuilderExtensions {
	extension(IApplicationBuilder app) {
		public IApplicationBuilder UseTransacto(params IPlugin[] additionalPlugins) {
			var plugins = additionalPlugins.Concat(Standard.Plugins).ToArray();

			return plugins.Aggregate(app, (inner, plugin) => inner.Map(
					$"/{plugin.Slug}", app => app
						.UseTransactoProblemDetails()
						.Use(async (context, next) => {
							using (LogContext.PushProperty("Plugin", plugin.Name)) {
								await next(context);
							}
						})
						.Use((context, next) => {
							context.RequestServices =
								app.ApplicationServices.GetRequiredKeyedService<IServiceProvider>(plugin);
							return next();
						})
						.UseRouting()
						.UseOutputCache()
						.UseEndpoints(e => {
							var group = e.MapGroup("");
							plugin.Configure(group);
							group.AddEndpointFilter(new ProblemDetailsEndpointFilter());
						})
						.UseProjectionStatus()
				))
				.Map("", inner => inner.UseTransactoIndex(plugins));
		}


		private void UseTransactoIndex(params IPlugin[] plugins) => app
			.UseTransactoProblemDetails()
			.UseRouting()
			.UseEndpoints(builder => {
				var resource = Index.Create(plugins);

				builder.MapGet("",
					([FromServices] IndexRepresentation representation) =>
						Results.Hal(resource, Checkpoint.None, representation));
			});

		private IApplicationBuilder UseTransactoProblemDetails() => app
			.UseExceptionHandler(new ExceptionHandlerOptions {
				AllowStatusCode404Response = false,
				ExceptionHandler = async context => {
					var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();

					if (exceptionFeature is null) {
						return;
					}

					var exception = exceptionFeature.Error;

					var activity = context.Features.Get<IHttpActivityFeature>()?.Activity;

					var env = context.RequestServices.GetRequiredService<IWebHostEnvironment>();

					var problemDetails = new ProblemDetails {
						Status = SelectStatusCode(exception), // your existing method
						Instance = $"{context.Request.Method} {context.Request.Path}",
						Extensions = {
							["requestId"] = context.TraceIdentifier,
							["activityId"] = activity?.Id
						}
					};

					if (env.IsDevelopment()) {
						problemDetails.Extensions["exception"] = new {
							type = exception.GetType().FullName,
							message = exception.Message,
							stackTrace = exception.StackTrace
						};
					}

					await Results.Problem(problemDetails).ExecuteAsync(context);

					return;

					static int SelectStatusCode(Exception ex) => ex switch {
						AccountNumberLengthOutOfRangeException or
							ArgumentException or
							ArgumentOutOfRangeException or
							AccountCannotBeCashAccountException or
							AccountExpectedCashFlowCategoryException or
							JsonException => 400,
						AccountNotFoundException or
							ChartOfAccountsNotFoundException or
							GeneralLedgerEntryNotFoundException => 404,
						AccountDeactivatedException or
							AccountExistsException or
							AccountNotFoundException or
							ChartOfAccountsAlreadyConfiguredException or
							ClosingDateBeforePeriodException or
							GeneralLedgerEntryAlreadyAssignedSequenceNumberException or
							ChartOfAccountsNotConfiguredException or
							GeneralLedgerEntryNotInBalanceException or
							GeneralLedgerEntryNotInPeriodException or
							GeneralLedgerEntryWasNotPostedException or
							GeneralLedgerEntryWasPostedException or
							PeriodClosingInProcessException or
							PeriodContainsUntransferredEntriesException or
							PeriodOpenException or
							TrialBalanceFailedException => 409,
						_ => 500
					};
				}
			})
			.UseStatusCodePages();
	}

	private class ProblemDetailsEndpointFilter : IEndpointFilter {
		public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context,
			EndpointFilterDelegate next) {
			var result = await next(context);
			if (result is not EmptyHttpResult || context.HttpContext.Response.StatusCode != 400) {
				return result;
			}

			var httpContext = context.HttpContext;
			var endpoint = httpContext.GetEndpoint();

			// Get parameter info from the endpoint metadata
			var parameters = endpoint?.Metadata
				.OfType<MethodInfo>()
				.FirstOrDefault()
				?.GetParameters() ?? [];

			var errors = new Dictionary<string, string[]>();

			foreach (var param in parameters) {
				// Check for [FromQuery] attribute
				var fromQueryAttr = param.GetCustomAttribute<FromQueryAttribute>();
				var bindingName = fromQueryAttr?.Name ?? param.Name!;

				string?[]? values = null;

				// Check if it's from query (has [FromQuery] or not in route)
				if (fromQueryAttr is not null ||
				    !httpContext.Request.RouteValues.TryGetValue(bindingName, out var value)) {
					// Get query values (could be multiple)
					var queryValues = httpContext.Request.Query[bindingName];
					if (queryValues.Count > 0) {
						values = queryValues.ToArray();
					}
				} else {
					// Get from route
					var routeValue = value?.ToString();
					if (!string.IsNullOrEmpty(routeValue)) {
						values = [routeValue];
					}
				}

				// If we found values, assume they failed to bind
				if (values is not { Length: > 0 }) continue;
				errors[bindingName] = values.Length == 1
					? [$"The value '{values[0]}' is not valid for {bindingName}."]
					: [
						$"The values [{string.Join(", ", values.Select(v => $"'{v}'"))}] are not valid for {bindingName}."
					];
			}

			var problemDetails = new ValidationProblemDetails(errors) {
				Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
				Title = "One or more validation errors occurred.",
				Status = 400,
				Instance = $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}",
				Extensions = {
					["traceId"] = context.HttpContext.TraceIdentifier
				}
			};

			// Return with proper content type
			return new ObjectResult(problemDetails) {
				StatusCode = 400,
				ContentTypes = { "application/problem+json" }
			};
		}
	}

	extension(IEndpointConventionBuilder app) {
		public IEndpointConventionBuilder CacheControl(RequestDelegate cachePolicy) =>
			app.AddEndpointFilter(async (context, next) => {
				context.HttpContext.Response.OnStarting(async static state => {
					var (context, cachePolicy) = ((HttpContext, RequestDelegate))state;

					await cachePolicy(context);
				}, (context.HttpContext, cachePolicy));

				return await next(context);
			});
	}
}
