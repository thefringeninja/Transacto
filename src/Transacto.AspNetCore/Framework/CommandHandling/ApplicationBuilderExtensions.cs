using System.Collections.Concurrent;
using System.Text.Json;
using Hallo;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Transacto;
using Transacto.Domain;
using Transacto.Framework.CommandHandling;
using Transacto.Framework.Http;
using Transacto.Messages;

// ReSharper disable CheckNamespace
namespace Microsoft.AspNetCore.Builder;
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
		JsonSerializerOptions serializerOptions, params Type[] commandTypes) {
		var map = commandTypes.ToDictionary(commandType => commandType.Name);
		var schemaCache = new JsonCommandSchemaCache(commandTypes);
		builder.MapPost(route, async (HttpRequest request, [FromServices] CommandDispatcher dispatcher) => {
			if (!MediaTypeHeaderValue.TryParse(request.ContentType, out var mediaType)) {
				return Results.Text($"No media type specified.",
					statusCode: 415);
			}

			if (!mediaType.MediaType.Equals("multipart/form-data", StringComparison.OrdinalIgnoreCase)) {
				return Results.Text($"Media type '{mediaType.MediaType}' not supported.",
					statusCode: 415);
			}

			if (!request.Form.TryGetValue("command", out var commandName)) {
				return Results.Text("No command type was specified.", statusCode: 400);
			}

			if (!map.TryGetValue(commandName.ToString(), out var commandType)) {
				return Results.Text($"The command type '{commandName}' was not recognized.", statusCode: 400);
			}

			if (request.Form.Files.Count != 1) {
				return Results.Text("No command was found on the request.", statusCode: 400);
			}

			await using var commandStream = request.Form.Files[0].OpenReadStream();
			var command = await JsonSerializer.DeserializeAsync(commandStream, commandType, serializerOptions);

			var checkpoint = await dispatcher.Handle(command!, request.HttpContext.RequestAborted);

			return Results.Extensions.CommandHandled(checkpoint);
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
