using System.Runtime.CompilerServices;
using System.Threading.Channels;
using KurrentDB.Client;

namespace Transacto.Infrastructure;

internal static class StreamSubscriptionResultExtensions {
	extension(KurrentDBClient.StreamSubscriptionResult subscription) {
		public async IAsyncEnumerable<SubscriptionControlMessage> Batch(int batchSize, TimeSpan period,
			[EnumeratorCancellation] CancellationToken cancellationToken) {
			var channel = Channel.CreateBounded<SubscriptionControlMessage>(new BoundedChannelOptions(batchSize) {
				FullMode = BoundedChannelFullMode.Wait,
				SingleWriter = false,
				SingleReader = true
			});

			await using var timer = new System.Threading.Timer(async state => {
				var channel = (state as Channel<SubscriptionControlMessage>)!;
				try {
					await channel.Writer.WriteAsync(SubscriptionControlMessage.Flush.Instance, cancellationToken);
				} catch (Exception ex) {
					channel.Writer.TryComplete(ex);
				}
			}, channel, period, Timeout.InfiniteTimeSpan);

			_ = Produce(cancellationToken);

			await foreach (var e in channel.Reader.ReadAllAsync(cancellationToken)) {
				yield return e;
			}

			yield break;

			async Task Produce(CancellationToken ct) {
				int count = 0;
				try {
					await foreach (var message in subscription.Messages.WithCancellation(ct)) {
						timer.Change(period, Timeout.InfiniteTimeSpan);
						await channel.Writer.WriteAsync(new SubscriptionControlMessage.SubscriptionMessage(message),
							ct);
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
}
