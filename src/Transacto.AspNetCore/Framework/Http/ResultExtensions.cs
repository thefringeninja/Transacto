using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Threading.Channels;
using Hallo;
using Microsoft.AspNetCore.Http.Features;

namespace Transacto.Framework.Http;

public static class ResultExtensions {
	extension(Results) {
		public static IResult SeeOther([StringSyntax(StringSyntaxAttribute.Uri)] string location) {
			return new SeeOtherResult(location);
		}

		public static IResult Hal<T>(Option<T> optionalResource, Checkpoint checkpoint, IHal hal) {
			return optionalResource switch {
				Some<T> some => Results.Hal(some.Value, checkpoint, hal),
				None => Results.Hal(default(T), checkpoint, hal, HttpStatusCode.NotFound),
			};
		}

		public static IResult Hal<T>(T? resource, Checkpoint checkpoint, IHal hal,
			HttpStatusCode statusCode = HttpStatusCode.OK) =>
			new HalResult<T>(resource, checkpoint, hal, statusCode);

		public static IResult CommandHandled(Checkpoint checkpoint) => new CommandHandledResult(checkpoint);

		public static IResult ObservableServerSentEvents<T>(IObservable<T> source) =>
			new ServerSentEventsResults<T>(source);
	}

	private class ServerSentEventsResults<T>(IObservable<T> source) : IResult {
		public async Task ExecuteAsync(HttpContext httpContext) {
			httpContext.Response.ContentType = "text/event-stream";
			httpContext.Response.Headers.CacheControl = "no-cache,no-store";
			httpContext.Response.Headers.Pragma = "no-cache";
			httpContext.Response.Headers.ContentEncoding = "identity";

			var bufferingFeature = httpContext.Features.GetRequiredFeature<IHttpResponseBodyFeature>();
			bufferingFeature.DisableBuffering();

			var channel = Channel.CreateBounded<T>(new BoundedChannelOptions(1) {
				SingleReader = true,
				SingleWriter = true,
				FullMode = BoundedChannelFullMode.DropOldest,
			});

			httpContext.Response.OnCompleted(c => {
				((Channel<T>)c).Writer.TryComplete();
				return Task.CompletedTask;
			}, channel);

			var lifetime = httpContext.RequestServices.GetRequiredService<IHostApplicationLifetime>();

			using var cts = CancellationTokenSource.CreateLinkedTokenSource(
				lifetime.ApplicationStopping,
				httpContext.RequestAborted);

			try {
				using var subscription = source.Subscribe(m => channel.Writer.TryWrite(m));

				await foreach (var m in channel.Reader.ReadAllAsync(cts.Token)) {
					await httpContext.Response.WriteAsync("data: ", cancellationToken: cts.Token);

					await httpContext.Response.WriteAsJsonAsync(m,
						TransactoHttpSerializerOptions.HalJsonSerializerOptions, cts.Token);
					await httpContext.Response.WriteAsync("\n\n", cancellationToken: cts.Token);
				}
			} catch (OperationCanceledException) {
				// Shutdown or client disconnect
			} catch (IOException) {
				// Client disconnected
			}
		}
	}
}
