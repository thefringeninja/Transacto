using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;
using DiffPlex.Renderer;
using Hallo;
using Microsoft.Extensions.DependencyInjection;
using Transacto.Framework.Http;
using Transacto.Framework.Http.Html;

namespace Transacto;

public static class AssertExtensions {
	private static readonly JsonSerializerOptions JsonSerializerOptions = JsonSerializerOptions =
		new JsonSerializerOptions() {
			WriteIndented = true
		};

	extension(Assert) {
		public static async Task HalResponseEqual<T, THal>(HalResponse<T> expected, HttpResponseMessage actual, Uri? siteRoot = null)
			where T : class
			where THal : class, IHal {

			await using var sp = new ServiceCollection()
				.AddSingleton<THal>()
				.AddSingleton(new SiteRoot(siteRoot ?? new UriBuilder().Uri))
				.BuildServiceProvider();

			var hal = await sp.GetRequiredService<THal>().RepresentationOfAsync(expected.State);

			var jExpected = JsonNode.Parse(JsonSerializer.Serialize(hal,
				options: TransactoHttpSerializerOptions.HalJsonSerializerOptions));

			var content = await actual.Content.ReadAsStringAsync();

			JsonNode? jActual;

			try {
				jActual = JsonNode.Parse(content);
			}
			catch(JsonException) {
				throw new Exception(content);
			}

			Assert.Equal("application/hal+json", actual.Content.Headers.ContentType?.MediaType);

			Assert.Multiple(
				() => Assert.Equal(expected.StatusCode, actual.StatusCode),
				() => {
					if (JsonNode.DeepEquals(jExpected, jActual)) {
						return;
					}

					var expected = jExpected?.ToJsonString(JsonSerializerOptions);
					var actual = jActual?.ToJsonString(JsonSerializerOptions);
					var diff = UnidiffRenderer.GenerateUnidiff(
						expected,
						actual,
						nameof(expected),
						nameof(actual));
					Assert.Fail(diff);
				});
		}
	}
}
