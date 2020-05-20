using System.Net;

namespace Transacto {
	public sealed class NotAcceptableResponse : Response {
		public static readonly Response Instance = new NotAcceptableResponse();

		private NotAcceptableResponse() {
			StatusCode = HttpStatusCode.NotAcceptable;
		}
	}
}
