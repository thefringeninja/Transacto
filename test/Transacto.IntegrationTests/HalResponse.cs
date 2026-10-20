using System.Net;
using Hallo;

namespace Transacto;

public record HalResponse<T>(T State, Link[] Links, HttpStatusCode StatusCode = HttpStatusCode.OK);
