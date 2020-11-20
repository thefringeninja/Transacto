using System.Net;

namespace Transacto {
	public sealed class PreconditionFailedResponse : Response {
		public static PreconditionFailedResponse Instance = new();

		private PreconditionFailedResponse() {
			StatusCode = HttpStatusCode.PreconditionFailed;
		}
	}
}
