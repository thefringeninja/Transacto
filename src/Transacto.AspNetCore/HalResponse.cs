using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Hallo;
using Hallo.Serialization;

namespace Transacto {
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
}
