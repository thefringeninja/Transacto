using EventStore.Client;

namespace Transacto.Framework;

public record Envelope(object Message, Position Position);
