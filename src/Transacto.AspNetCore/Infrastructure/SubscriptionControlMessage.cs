using KurrentDB.Client;

namespace Transacto.Infrastructure;

internal abstract record SubscriptionControlMessage {
	public sealed record Flush : SubscriptionControlMessage {
		public static readonly Flush Instance = new();
	}

	public sealed record SubscriptionMessage(StreamMessage StreamMessage) : SubscriptionControlMessage;
}
