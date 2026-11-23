namespace Transacto.Framework.Http;

/// <summary>
///     Thrown from an endpoint when a request's precondition (such as If-Match) no longer holds. The plugin
///     endpoint filter turns it into a 412.
/// </summary>
public class PreconditionFailedException(string message) : Exception(message);
