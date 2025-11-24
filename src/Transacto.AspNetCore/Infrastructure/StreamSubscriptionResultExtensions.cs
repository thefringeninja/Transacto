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

		var timer = new Timer(state => {
			var channel = (Channel<SubscriptionControlMessage>)state!;
			channel.Writer.TryWrite(SubscriptionControlMessage.Flush.Instance);
		}, channel, period, period);

		Produce();

		return channel.Reader.ReadAllAsync(cancellationToken);

		async void Produce() {
			int count = 0;
			try {
				await foreach (var message in subscription.Messages.WithCancellation(cancellationToken)) {
					timer.Change(period, period);
					await channel.Writer.WriteAsync(new SubscriptionControlMessage.SubscriptionMessage(message),
						cancellationToken);
					if (count++ >= batchSize) {
						count = 0;
						await channel.Writer.WriteAsync(SubscriptionControlMessage.Flush.Instance, cancellationToken);
					}
				}
			} catch (Exception ex) {
				channel.Writer.TryComplete(ex);
				return;
			}

			channel.Writer.TryComplete();
		}
	}
}
