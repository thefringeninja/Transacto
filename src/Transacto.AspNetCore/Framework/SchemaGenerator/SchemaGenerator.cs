using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Hallo;
using Microsoft.AspNetCore.Mvc;
using NJsonSchema;
using NJsonSchema.Generation;
using NJsonSchema.Generation.TypeMappers;
using NodaTime;
using Transacto.Framework.Http;

namespace Transacto.Framework.SchemaGenerator;

public class SchemaGenerator : IPlugin {
	public string Name => nameof(SchemaGenerator);
	public IEnumerable<Type> MessageTypes { get; } = [];
	public IEnumerable<LinkRelation> LinkRelations { get; } = [];

	public void Configure(IEndpointRouteBuilder builder) {
		builder.MapGet("", ([FromQuery] string? t) => {
			var type = Type.GetType(t ?? string.Empty);
			return Results.Hal(type, Checkpoint.None, new SchemaRepresentation(),
				statusCode: type is null ? HttpStatusCode.NotFound : HttpStatusCode.OK);
		});
	}

	private class SchemaRepresentation : Hal<Type>, IHalState<Type> {
		private readonly SystemTextJsonSchemaGeneratorSettings _settings;

		public SchemaRepresentation() {
			_settings = new SystemTextJsonSchemaGeneratorSettings {
				SerializerOptions = {
					PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
					Converters = { new JsonStringEnumConverter() }
				},
				TypeMappers = {
					new PrimitiveTypeMapper(typeof(LocalDate), s => {
						s.Type = JsonObjectType.String;
						s.Format = "date";
					}),
					new PrimitiveTypeMapper(typeof(OffsetDateTime), s => {
						s.Type = JsonObjectType.String;
						s.Format = "date-time";
					}),
					new PrimitiveTypeMapper(typeof(Period), s => {
						s.Type = JsonObjectType.String;
						s.Pattern = """
						            /^(-?)P(?=\d|T\d)(?:(\d+)Y)?(?:(\d+)M)?(?:(\d+)[DW])?(?:T(?:(\d+)H)?(?:(\d+)M)?(?:(\d+(?:\.\d+)?)S)?)?$/
						            """;
					}),
				}
			};
		}

		public object StateFor(Type? resource) => resource is null
			? new object()
			: JsonDocument.Parse(JsonSchema.FromType(resource, _settings).ToJson()).RootElement;
	}
}
