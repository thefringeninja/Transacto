using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Hallo;
using Hallo.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;
using RazorLight;
using Transacto.Framework;
using Transacto.Views;

namespace Transacto {
	public class HalResponse : Response {
		private static readonly object EmptyBody = new object();
		private static readonly MediaType HalJson = new MediaType("application/hal+json");
		private static readonly MediaType Html = new MediaType("text/html");

		private readonly Response _inner;

		public override ResponseHeaders Headers => _inner.Headers;
		public override HttpStatusCode StatusCode { get => _inner.StatusCode; set => _inner.StatusCode = value; }

		public HalResponse(HttpRequest request, IHal hal, ETag etag = default) : this(request, hal,
			etag, null!) {
		}

		public HalResponse(HttpRequest request, IHal hal, ETag etag, Optional<object> resource) {
			_inner = request.Headers["accept"].Count == 0
				? new HalJsonResponse(hal, resource)
				: request.Headers["accept"].Select(MediaType).Select(Negotiate).FirstOrDefault() ??
				  NotAcceptableResponse.Instance;

			if (etag != ETag.None) {
				_inner.Headers.ETag = new EntityTagHeaderValue($@"""{etag.ToString()}""");
			}

			Response Negotiate(MediaType m) =>
				(HalJson.IsSubsetOf(m), Html.IsSubsetOf(m) || m.SubTypeSuffix == "html") switch {
					(true, false) => new HalJsonResponse(hal, resource),
					(false, true) => new HalHtmlResponse(hal, resource),
					_ => NotAcceptableResponse.Instance
				};

			static MediaType MediaType(string x) => new MediaType(x ?? string.Empty);
		}

		protected internal override ValueTask WriteBody(Stream stream, CancellationToken cancellationToken) => _inner
			.WriteBody(stream, cancellationToken);

		private sealed class HalHtmlResponse : Response {
			private static readonly ConcurrentDictionary<Assembly, RazorLightEngine> Engines =
				new ConcurrentDictionary<Assembly, RazorLightEngine>();

			private static readonly MediaTypeHeaderValue ContentType = new MediaTypeHeaderValue("text/html");

			private readonly object _resource;
			private readonly IHal _hal;

			public HalHtmlResponse(IHal hal, Optional<object> resource) {
				_resource = resource.HasValue ? resource.Value : EmptyBody;
				_hal = hal;

				Headers.ContentType = ContentType;
			}

			protected internal override async ValueTask WriteBody(Stream stream, CancellationToken cancellationToken) {
				var representation = await _hal.RepresentationOfAsync(_resource);
				await stream.WriteAsync(Encoding.UTF8.GetBytes("<html><body>"), cancellationToken);

				await stream.WriteAsync(Encoding.UTF8.GetBytes(await Render(typeof(Links), representation)),
					cancellationToken);
				await stream.WriteAsync(Encoding.UTF8.GetBytes(await Render(_hal.GetType(), representation)),
					cancellationToken);

				await stream.WriteAsync(Encoding.UTF8.GetBytes("</body></html>"), cancellationToken);
			}

			private static Task<string> Render(Type type, HalRepresentation representation) => Engines
				.GetOrAdd(type.Assembly, assembly => new RazorLightEngineBuilder()
					.UseEmbeddedResourcesProject(assembly)
					.UseMemoryCachingProvider()
					.Build())
				.CompileRenderAsync(type.FullName, representation.State);
		}

		private sealed class HalJsonResponse : Response {
			private static readonly JsonSerializerOptions SerializerOptions
				= new JsonSerializerOptions {
					Converters = {
						new LinksConverter(),
						new HalRepresentationConverter()
					},

					PropertyNamingPolicy = JsonNamingPolicy.CamelCase
				};

			private static readonly MediaTypeHeaderValue ContentType = new MediaTypeHeaderValue("application/hal+json");

			private readonly object _resource;
			private readonly IHal _hal;

			public HalJsonResponse(IHal hal, Optional<object> resource) {
				_resource = resource.HasValue ? resource.Value : EmptyBody;
				_hal = hal;
				Headers.ContentType = ContentType;
			}

			protected internal override async ValueTask WriteBody(Stream stream, CancellationToken cancellationToken) {
				var representation = await _hal.RepresentationOfAsync(_resource);
				await JsonSerializer.SerializeAsync(stream, representation, SerializerOptions, cancellationToken);
			}

			private class EnumerableOfDictionaryEntryConverter : JsonConverter<IEnumerable<DictionaryEntry>> {
				public override IEnumerable<DictionaryEntry> Read(ref Utf8JsonReader reader, Type typeToConvert,
					JsonSerializerOptions options) => throw new NotSupportedException();

				public override void Write(Utf8JsonWriter writer, IEnumerable<DictionaryEntry> value,
					JsonSerializerOptions options) {
					foreach (var entry in value) {
						//writer.WritePropertyName(entry.Key.ToString());
						//writer.WriteNullValue();
					}
				}
			}
		}
	}
}
