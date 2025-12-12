using System.Threading.Channels;
using KurrentDB.Client;

namespace Transacto.Infrastructure;

internal static class StreamSubscriptionResultExtensions {
	public static IAsyncEnumerable<SubscriptionControlMessage> Batch(
		this KurrentDBClient.StreamSubscriptionResult subscription,
		int batchSize, TimeSpan period, CancellationToken cancellationToken) {
		var channel = Channel.CreateBounded<SubscriptionControlMessage>(new BoundedChannelOptions(batchSize) {
			FullMode = BoundedChannelFullMode.Wait,
			SingleWriter = false,
			SingleReader = true
		});

		var timer = new Timer(async state => {
			var (channel, ct) = ((Channel<SubscriptionControlMessage>, CancellationToken))state!;
			await channel.Writer.WriteAsync(SubscriptionControlMessage.Flush.Instance, ct);
		}, (channel, cancellationToken), period, Timeout.InfiniteTimeSpan);

		Produce(cancellationToken);

		return channel.Reader.ReadAllAsync(cancellationToken);

		async void Produce(CancellationToken ct) {
			int count = 0;
			try {
				await foreach (var message in subscription.Messages.WithCancellation(ct)) {
					timer.Change(period, Timeout.InfiniteTimeSpan);
					await channel.Writer.WriteAsync(new SubscriptionControlMessage.SubscriptionMessage(message), ct);
					if (count++ < batchSize) {
						continue;
					}
					count = 0;
					await channel.Writer.WriteAsync(SubscriptionControlMessage.Flush.Instance, ct);
				}
			} catch (Exception ex) {
				channel.Writer.TryComplete(ex);
				return;
			}

			channel.Writer.TryComplete();
		}
	}
}
