using System.Net;

namespace Transacto {
	public sealed class PreconditionFailedResponse : Response {
		public static PreconditionFailedResponse Instance = new PreconditionFailedResponse();

		private PreconditionFailedResponse() {
			StatusCode = HttpStatusCode.PreconditionFailed;
		}
	}
}
