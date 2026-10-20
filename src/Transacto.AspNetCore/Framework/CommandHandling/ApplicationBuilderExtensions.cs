using System.Text.Json;
using Hallo;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Transacto;
using Transacto.Domain;
using Transacto.Framework;
using Transacto.Framework.CommandHandling;
using Transacto.Framework.Http;
using Transacto.Messages;

// ReSharper disable CheckNamespace
namespace Microsoft.AspNetCore.Builder;
// ReSharper restore CheckNamespace

public static partial class ApplicationBuilderExtensions {
	extension(IEndpointRouteBuilder builder) {
		public IEndpointRouteBuilder MapCommands(string route, params Type[] commandTypes) =>
			builder
				.MapCommandsInternal(route, TransactoSerializerOptions.Messages, commandTypes);

		public IEndpointRouteBuilder MapBusinessTransaction<T>(string route) where T : IBusinessTransaction =>
			builder.MapCommandsInternal(route, TransactoHttpSerializerOptions.BusinessTransactions(typeof(T)),
				[typeof(PostGeneralLedgerEntry)]);

		private IEndpointRouteBuilder MapCommandsInternal(string route,
			JsonSerializerOptions serializerOptions, params Type[] commandTypes) {
			var map = commandTypes.ToDictionary(commandType => commandType.Name);
			var schemaCache = new JsonCommandSchemataCache(commandTypes);
			builder.MapPost(route, async (HttpRequest request, [FromServices] CommandDispatcher dispatcher) => {
				if (!MediaTypeHeaderValue.TryParse(request.ContentType, out var mediaType)) {
					return Results.Text("No media type specified.", statusCode: 415);
				}

				if (!mediaType.MediaType.Equals("multipart/form-data", StringComparison.OrdinalIgnoreCase)) {
					return Results.Text($"Media type '{mediaType.MediaType}' not supported.", statusCode: 415);
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

				if (command is null) {
					return Results.Text("The command was empty", statusCode: 400);
				}

				var checkpoint = await dispatcher.Handle(command, request.HttpContext.RequestAborted);

				return Results.CommandHandled(checkpoint);
			});

			builder.MapGet($"{route}/commands/{{commandType}}", (string commandType, CancellationToken ct) => {
				var schema = schemaCache[commandType];

				return Results.Hal(schema, Checkpoint.None, new JsonCommandSchemaRepresentation());
			});

			builder.MapGet($"{route}/commands", (HttpRequest request) =>
				Results.Hal(schemaCache, Checkpoint.None, new JsonCommandSchemataRepresentation()));

			return builder;
		}
	}

	private class JsonCommandSchemaRepresentation : Hal<JsonCommandSchema>, IHalState<JsonCommandSchema> {
		public object StateFor(JsonCommandSchema resource) => resource;
	}

	private class JsonCommandSchemataRepresentation : Hal<JsonCommandSchemataCache>, IHalState<JsonCommandSchemataCache> {
		public object StateFor(JsonCommandSchemataCache resource) =>
			new {
				forms = Array.ConvertAll(resource.CommandTypes, type => resource[type])
			};
	}
}
