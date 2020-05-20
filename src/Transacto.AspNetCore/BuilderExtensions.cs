using System;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using EventStore.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Transacto.Domain;
using Transacto.Framework.CommandHandling;
using Transacto.Infrastructure;
using Transacto.Messages;

namespace Transacto {
	static partial class BuilderExtensions {
		public static IEndpointRouteBuilder MapGet(this IEndpointRouteBuilder builder, string route,
			Func<HttpContext, ValueTask<Response>> getResponse) {
			var separator = new[] {'/'};
			builder.MapGet(route, async context => {
				var response = await getResponse(context);
				if (TryParsePosition(response.Headers.ETag, out var responsePosition)) {
					var positions = context.Request.GetTypedHeaders()
						.IfMatch
						.Where(etag => etag.Tag.HasValue)
						.Select(etag => TryParsePosition(etag, out var position) ? position : Position.Start)
						.OrderByDescending(p => p);

					foreach (var requestedPosition in positions) {
						if (responsePosition >= requestedPosition) {
							await response.Write(context.Response);
							return;
						}
					}

					await PreconditionFailedResponse.Instance.Write(context.Response);

					return;
				}

				await response.Write(context.Response);
			});

			return builder;

			bool TryParsePosition(EntityTagHeaderValue etag, out Position position) {
				position = default;
				var value = etag?.Tag.ToString();
				if (value == "*") {
					position = Position.Start;
					return true;
				}

				var parts = value?.Split(separator, 2) ?? Array.Empty<string>();

				if (parts.Length != 2 || !ulong.TryParse(parts[0][1..], out var p) || !ulong.TryParse(parts[1][..^1], out var c)) {
					return false;
				}

				position = new Position(p, c);
				return true;
			}
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

		public static IEndpointRouteBuilder MapCommands(this IEndpointRouteBuilder builder, string route,
			params Type[] commandTypes) =>
			builder.MapCommandsInternal(route, TransactoSerializerOptions.Commands, commandTypes);

		public static IEndpointRouteBuilder MapBusinessTransaction<T>(this IEndpointRouteBuilder builder, string route)
			where T : IBusinessTransaction =>
			builder.MapCommandsInternal(route, TransactoSerializerOptions.BusinessTransactions(typeof(T)),
				typeof(PostGeneralLedgerEntry));

		private static IEndpointRouteBuilder MapCommandsInternal(this IEndpointRouteBuilder builder, string route,
			JsonSerializerOptions serializerOptions,
			params Type[] commandTypes) {
			var dispatcher = new CommandDispatcher(builder.ServiceProvider.GetServices<CommandHandlerModule>());

			var map = commandTypes.ToDictionary(commandType => commandType.Name);

			builder.MapPost(route, async context => {
				if (!MediaTypeHeaderValue.TryParse(context.Request.ContentType, out var mediaType) ||
				    !mediaType.MediaType.Equals("multipart/form-data", StringComparison.OrdinalIgnoreCase)) {
					return new Response {StatusCode = HttpStatusCode.UnsupportedMediaType};
				}

				if (!context.Request.Form.TryGetValue("command", out var commandName)) {
					return new TextResponse($"No command type was specified.") {
						StatusCode = HttpStatusCode.BadRequest
					};
				}

				if (!map.TryGetValue(commandName, out var commandType)) {
					return new TextResponse($"The command type '{commandName}' was not recognized.") {
						StatusCode = HttpStatusCode.BadRequest
					};
				}

				if (context.Request.Form.Files.Count != 1) {
					return new TextResponse("No command was found on the request.")
						{StatusCode = HttpStatusCode.BadRequest};
				}

				await using var commandStream = context.Request.Form.Files[0].OpenReadStream();
				var command = await JsonSerializer.DeserializeAsync(commandStream, commandType,
					serializerOptions);

				var position = await dispatcher.Handle(command, context.RequestAborted);

				return new CommandHandledResponse(position);
			});

			return builder;
		}
	}
}
