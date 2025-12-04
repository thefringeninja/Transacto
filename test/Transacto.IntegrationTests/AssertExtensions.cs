using System.Text.Json;
using System.Text.Json.Nodes;
using DiffPlex.Renderer;
using Hallo;
using Transacto.Framework.Http;

namespace Transacto;

public static class AssertExtensions {
	private static readonly JsonSerializerOptions JsonSerializerOptions = JsonSerializerOptions =
		new JsonSerializerOptions() {
			WriteIndented = true
		};

	extension(Assert) {
		public static async Task HalResponseEqual<T, THal>(HalResponse<T> expected, HttpResponseMessage actual)
			where THal : IHal, new() {
			var hal = await new THal().RepresentationOfAsync(expected.State);

			var jExpected = JsonNode.Parse(JsonSerializer.Serialize(hal,
				options: TransactoHttpSerializerOptions.HalJsonSerializerOptions));

			var jActual = JsonNode.Parse(await actual.Content.ReadAsStringAsync());

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
