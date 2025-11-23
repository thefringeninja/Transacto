using System.Net;

namespace Transacto;

public record HalResponse<T>(T Resource, HttpStatusCode StatusCode = HttpStatusCode.OK);
