using System.Net;

namespace Transacto.Framework.Http; 

public sealed class PreconditionFailedResponse : Response {
	public static PreconditionFailedResponse Instance = new();

	private PreconditionFailedResponse() {
		StatusCode = HttpStatusCode.PreconditionFailed;
	}
}