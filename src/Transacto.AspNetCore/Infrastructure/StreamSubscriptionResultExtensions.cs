using System.Runtime.CompilerServices;
using System.Threading.Channels;
using KurrentDB.Client;
using SqlStreamStore.Imports.AsyncEx.Nito.AsyncEx.Coordination;

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

			var activity = new AsyncAutoResetEvent();

			_ = Produce(cancellationToken);
			_ = DebounceFlushLoop(cancellationToken);
			await foreach (var e in channel.Reader.ReadAllAsync(cancellationToken)) {
				yield return e;
			}

			async Task DebounceFlushLoop(CancellationToken ct) {
				while (!ct.IsCancellationRequested) {
					// 1. Sleep until *some event arrives*
					await activity.WaitAsync(ct);

					// 2. Now wait 200 ms of silence
					while (true) {
						var delayTask = Task.Delay(period, ct);
						var activityTask = activity.WaitAsync(ct);

						var completed = await Task.WhenAny(delayTask, activityTask);

						if (completed == delayTask) {
							// Silence for 200 ms -> flush
							await channel.Writer.WriteAsync(SubscriptionControlMessage.Flush.Instance, ct);
							break; // go back to waiting for new activity
						}

						// else: activity happened -> restart 200ms timer
					}
				}
			}

			async Task Produce(CancellationToken ct) {
				int count = 0;
				try {
					await foreach (var message in subscription.Messages.WithCancellation(ct)) {
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
