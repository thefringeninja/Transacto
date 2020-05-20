using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;

namespace Transacto {
	public static class Negotiate {
		public static Response Content(HttpRequest request, params Response[] responses) {
			foreach (var acceptHeader in request.Headers.GetCommaSeparatedValues("accept")
				.Select(MediaTypeWithQualityHeaderValue.Parse)
				.OrderByDescending(h => h.Quality)
				.Select(h => new Microsoft.Net.Http.Headers.MediaTypeHeaderValue(h.MediaType))) {
				var match = responses.FirstOrDefault(response => response.Headers.ContentType
					.IsSubsetOf(acceptHeader));
				if (match == null) {
					continue;
				}

				return match;
			}

			return new Response {
				StatusCode = HttpStatusCode.NotAcceptable
			};
		}
	}
}
