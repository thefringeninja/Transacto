using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Hallo;
using Hallo.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

#nullable enable
namespace SomeCompany.Framework.Http {
	public class NotFoundResponse : Response {
		public NotFoundResponse() {
			StatusCode = HttpStatusCode.NotFound;
		}
		protected override ValueTask WriteBody(Stream stream, CancellationToken cancellationToken = default) =>
			new ValueTask(Task.CompletedTask);
	}

	public class HalResponse : Response {
		private static readonly object EmptyBody = new object();
		private readonly object _resource;
		private readonly IHal _hal;

		private static readonly JsonSerializerOptions SerializerOptions
			= new JsonSerializerOptions {
				Converters = {
					new LinksConverter(),
					new HalRepresentationConverter()
				},
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase
			};

		public HalResponse(IHal hal, object? resource = null) {
			_resource = resource ?? EmptyBody;
			_hal = hal;
			Headers.Add(("content-type", "application/hal+json"));
		}

		protected override async ValueTask WriteBody(Stream stream, CancellationToken cancellationToken = default) {
			var representation = await _hal.RepresentationOfAsync(_resource);
			await JsonSerializer.SerializeAsync(stream, representation, SerializerOptions, cancellationToken);
		}
	}

	public class Response {
		public IList<(string, StringValues)> Headers { get; }
		public HttpStatusCode StatusCode { get; set; }

		public Response() {
			Headers = new List<(string, StringValues)>();
			StatusCode = HttpStatusCode.OK;
		}

		public ValueTask Write(HttpResponse response) {
			response.StatusCode = (int)StatusCode;

			foreach (var (key, value) in Headers) {
				response.Headers.AppendCommaSeparatedValues(key, value);
			}

			return WriteBody(response.Body, response.HttpContext.RequestAborted);
		}

		protected virtual ValueTask WriteBody(Stream stream, CancellationToken cancellationToken = default) =>
			new ValueTask(Task.CompletedTask);
	}
}
