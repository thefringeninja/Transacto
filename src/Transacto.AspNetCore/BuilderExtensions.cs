using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Hallo;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using SomeCompany.Framework.Http;
using Transaction.AspNetCore;
using Transacto.Domain;
using Transacto.Framework;
using Transacto.Infrastructure;
using Transacto.Messages;

// ReSharper disable CheckNamespace
namespace Microsoft.AspNetCore.Builder {
	// ReSharper restore CheckNamespace

	internal class ChartOfAccountRepresentation : Hal<SortedDictionary<string, string>>,
		IHalLinks<SortedDictionary<string, string>>,
		IHalState<SortedDictionary<string, string>> {
		public IEnumerable<Link> LinksFor(SortedDictionary<string, string> resource) {
			yield break;
		}

		public object StateFor(SortedDictionary<string, string> resource) => resource;
	}

	public static class BuilderExtensions {
		public static IApplicationBuilder UseTransacto(this IApplicationBuilder builder) {
			var readModel = builder.ApplicationServices.GetRequiredService<InMemoryReadModel>();
			return builder
				.Map("/chart-of-accounts",
					inner => inner
						.UseRouting()
						.UseEndpoints(endpoints => endpoints
							.MapGet(string.Empty, (CancellationToken ct) => {
								var response =
									!readModel.TryGet<IDictionary<int, (string, bool)>, IDictionary<string, string>>(
										nameof(ChartOfAccounts),
										value => new SortedDictionary<string, string>(
											value.ToDictionary(x => x.Key.ToString(), x => x.Value.Item1)),
										out var chartOfAccounts)
										? (Response)new NotFoundResponse()
										: new HalResponse(new ChartOfAccountRepresentation(), chartOfAccounts);


								return new ValueTask<Response>(response);
							})
							.MapCommands(string.Empty,
								typeof(DefineAccount),
								typeof(RenameAccount),
								typeof(DeactivateAccount),
								typeof(ReactivateAccount))));
		}

		public static IEndpointRouteBuilder MapGet(this IEndpointRouteBuilder builder, string route,
			Func<CancellationToken, ValueTask<Response>> getResponse) {
			var routeTemplate = TemplateParser.Parse(route);
			var argumentCount = routeTemplate.Parameters.Count(p => p.IsParameter);

			if (argumentCount != 0) {
				throw new Exception();
			}

			return builder.MapGet<object>(route, values => null, (_, ct) => getResponse(ct));
		}

		public static IEndpointRouteBuilder MapGet(this IEndpointRouteBuilder builder, string route,
			Func<HttpContext, ValueTask<Response>> getResponse) {
			builder.MapGet(route, async context => {
				var response = await getResponse(context);

				await response.Write(context.Response);
			});

			return builder;
		}

		public static IEndpointRouteBuilder MapPost<T>(this IEndpointRouteBuilder builder, string route,
			Func<HttpContext, T, ValueTask<Response>> getResponse) {
			builder.MapPost(route, async context => {
				var request = await JsonSerializer.DeserializeAsync<T>(context.Request.Body);

				var response = await getResponse(context, request);

				await response.Write(context.Response);
			});
			return builder;
		}

		public static IEndpointRouteBuilder MapPost(this IEndpointRouteBuilder builder, string route,
			Func<HttpContext, ValueTask<Response>> getResponse) {
			builder.MapPost(route, async context => {
				var response = await getResponse(context);

				await response.Write(context.Response);
			});
			return builder;
		}


		public static IEndpointRouteBuilder MapPut<T>(this IEndpointRouteBuilder builder, string route,
			Func<HttpContext, T, ValueTask<Response>> getResponse) {
			builder.MapPut(route, async context => {
				var request = await JsonSerializer.DeserializeAsync<T>(context.Request.Body);

				var response = await getResponse(context, request);

				await response.Write(context.Response);
			});
			return builder;
		}

		public static IEndpointRouteBuilder MapGet<T>(this IEndpointRouteBuilder builder, string route,
			Func<T, CancellationToken, ValueTask<Response>> getResponse) {
			var routeTemplate = TemplateParser.Parse(route);
			var argumentCount = routeTemplate.Parameters.Count(p => p.IsParameter);

			if (argumentCount == 0) {
				throw new Exception();
			}

			if (argumentCount == 1) {
				return builder.MapGet(route, values => (T)values[0], getResponse);
			}

			var createDtoMethod = typeof(T).GetMethods(BindingFlags.Static | BindingFlags.Public)
				.Single(mi => mi.Name == "Create" &&
				              mi.IsGenericMethod &&
				              mi.GetGenericArguments().Length == argumentCount);

			return builder.MapGet(route, values => (T)createDtoMethod.Invoke(null, values), getResponse);
		}

		public static IEndpointRouteBuilder MapCommands(this IEndpointRouteBuilder builder, string route,
			params Type[] commandTypes) {
			var dispatcher = new CommandDispatcher(builder.ServiceProvider.GetServices<CommandHandlerModule>());
			var map = commandTypes.ToDictionary(commandType => commandType.Name);

			builder.MapPost(route, async context => {
				if (!MediaTypeHeaderValue.TryParse(context.Request.ContentType, out var mediaType) ||
				    !mediaType.MediaType.Equals("multipart/form-data", StringComparison.OrdinalIgnoreCase)) {
					return new Response {StatusCode = HttpStatusCode.UnsupportedMediaType};
				}

				if (context.Request.Form.Files.Count != 1 ||
				    !context.Request.Form.TryGetValue("command", out var commandName) ||
				    !map.TryGetValue(commandName, out var commandType)) {
					return new Response {StatusCode = HttpStatusCode.BadRequest};
				}

				await using var commandStream = context.Request.Form.Files[0].OpenReadStream();
				var command = await JsonSerializer.DeserializeAsync(commandStream, commandType,
					TransactoSerializerOptions.CommandSerializerOptions());

				await dispatcher.Handle(command, context.RequestAborted);

				return new Response();
			});

			return builder;
		}

		public static IEndpointRouteBuilder MapBusinessTransaction<T>(this IEndpointRouteBuilder builder, string route)
			where T : IBusinessTransaction {
			var dispatcher = new CommandDispatcher(builder.ServiceProvider.GetServices<CommandHandlerModule>());
			var serializerOptions = TransactoSerializerOptions.CommandSerializerOptions(typeof(T));

			builder.MapPost(route, async context => {
				if (!MediaTypeHeaderValue.TryParse(context.Request.ContentType, out var mediaType) ||
				    !mediaType.MediaType.Equals("multipart/form-data", StringComparison.OrdinalIgnoreCase)) {
					return new Response {StatusCode = HttpStatusCode.UnsupportedMediaType};
				}

				if (context.Request.Form.Files.Count != 1 ||
				    !context.Request.Form.TryGetValue("command", out var commandName) ||
				    commandName != nameof(PostGeneralLedgerEntry)) {
					return new Response {StatusCode = HttpStatusCode.BadRequest};
				}

				await using var commandStream = context.Request.Form.Files[0].OpenReadStream();
				var command = await JsonSerializer.DeserializeAsync(commandStream, typeof(PostGeneralLedgerEntry),
					serializerOptions);

				await dispatcher.Handle(command, context.RequestAborted);

				return new Response();
			});

			return builder;
		}

		private static IEndpointRouteBuilder MapGet<T>(this IEndpointRouteBuilder builder, string route,
			Func<object[], T> getDto, Func<T, CancellationToken, ValueTask<Response>> getResponse) {
			builder.MapMethods(route, new[] {HttpMethod.Get.Method}, async context => {
				var dto = getDto(context.GetRouteData().Values.Values.ToArray());

				var response = await getResponse(dto, context.RequestAborted);

				await response.Write(context.Response);
			});

			return builder;
		}
	}
}
