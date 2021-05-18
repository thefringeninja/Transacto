using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using Hallo;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Transacto;
using Transacto.Domain;
using Transacto.Framework.CommandHandling;
using Transacto.Messages;

// ReSharper disable CheckNamespace
namespace Microsoft.AspNetCore.Builder {
	// ReSharper restore CheckNamespace

	public static partial class ApplicationBuilderExtensions {
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
			var map = commandTypes.ToDictionary(commandType => commandType.Name);
			var schemaCache = new JsonCommandSchemaCache(commandTypes);
			CommandDispatcher? dispatcher = null;

			builder.MapPost(route, async context => {
				dispatcher ??= new CommandDispatcher(context.RequestServices.GetServices<CommandHandlerModule>());

				if (!MediaTypeHeaderValue.TryParse(context.Request.ContentType, out var mediaType) ||
				    !mediaType.MediaType.Equals("multipart/form-data", StringComparison.OrdinalIgnoreCase)) {
					return new Response {
						StatusCode = HttpStatusCode.UnsupportedMediaType
					};
				}

				if (!context.Request.Form.TryGetValue("command", out var commandName)) {
					return new TextResponse("No command type was specified.") {
						StatusCode = HttpStatusCode.BadRequest
					};
				}

				if (!map.TryGetValue(commandName, out var commandType)) {
					return new TextResponse($"The command type '{commandName}' was not recognized.") {
						StatusCode = HttpStatusCode.BadRequest
					};
				}

				if (context.Request.Form.Files.Count != 1) {
					return new TextResponse("No command was found on the request.") {
						StatusCode = HttpStatusCode.BadRequest
					};
				}

				await using var commandStream = context.Request.Form.Files[0].OpenReadStream();
				var command = await JsonSerializer.DeserializeAsync(commandStream, commandType, serializerOptions);

				var checkpoint = await dispatcher.Handle(command!, context.RequestAborted);

				return new CommandHandledResponse(checkpoint);
			});

			return builder;
		}

		private class JsonCommandSchemaRepresentation : Hal<JsonCommandSchemaCache>, IHalState<JsonCommandSchemaCache> {
			public object StateFor(JsonCommandSchemaCache resource) {
				return new {
					forms = Array.ConvertAll(resource.CommandTypes, type => resource[type].RootElement)
				};
			}
		}

		private class JsonCommandSchemaCache {
			private readonly ConcurrentDictionary<Type, JsonDocument> _scripts;

			public Type[] CommandTypes { get; }
			public JsonDocument this[Type commandType] => GetScript(commandType);

			public JsonCommandSchemaCache(params Type[] commandTypes) {
				CommandTypes = commandTypes;
				_scripts = new ConcurrentDictionary<Type, JsonDocument>();
			}

			private JsonDocument GetScript(Type commandType) {
				if (!CommandTypes.Contains(commandType)) {
					throw new InvalidOperationException();
				}

				return _scripts.GetOrAdd(commandType,
					key => {
						using var stream =
							commandType.Assembly.GetManifestResourceStream(commandType, $"{key.Name}.schema.json");
						if (stream == null) {
							throw new Exception($"Embedded resource, {key}, not found. BUG!");
						}

						using StreamReader reader = new(stream);

						return JsonDocument.Parse(reader.ReadToEnd());
					});
			}
		}
	}
}
