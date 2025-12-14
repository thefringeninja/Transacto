using Grpc.Core;
using Grpc.Core.Interceptors;
using System.Diagnostics;

namespace Transacto.Infrastructure.EventStore;

/// <summary>
/// gRPC interceptor that extends the deadline to 5 minutes when a debugger is attached.
/// This prevents timeouts during debugging sessions.
/// </summary>
public class DebuggerDeadlineInterceptor : Interceptor {
	private static readonly TimeSpan DebugDeadline = TimeSpan.FromMinutes(5);

	public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
		TRequest request,
		ClientInterceptorContext<TRequest, TResponse> context,
		AsyncUnaryCallContinuation<TRequest, TResponse> continuation) {
		var modifiedContext = ModifyDeadlineIfDebugging(context);
		return continuation(request, modifiedContext);
	}

	public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(
		ClientInterceptorContext<TRequest, TResponse> context,
		AsyncClientStreamingCallContinuation<TRequest, TResponse> continuation) {
		var modifiedContext = ModifyDeadlineIfDebugging(context);
		return continuation(modifiedContext);
	}

	public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(
		TRequest request,
		ClientInterceptorContext<TRequest, TResponse> context,
		AsyncServerStreamingCallContinuation<TRequest, TResponse> continuation) {
		var modifiedContext = ModifyDeadlineIfDebugging(context);
		return continuation(request, modifiedContext);
	}

	public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(
		ClientInterceptorContext<TRequest, TResponse> context,
		AsyncDuplexStreamingCallContinuation<TRequest, TResponse> continuation) {
		var modifiedContext = ModifyDeadlineIfDebugging(context);
		return continuation(modifiedContext);
	}

	private ClientInterceptorContext<TRequest, TResponse> ModifyDeadlineIfDebugging<TRequest, TResponse>(
		ClientInterceptorContext<TRequest, TResponse> context)
		where TRequest : class
		where TResponse : class {
		if (!Debugger.IsAttached) {
			return context;
		}

		var deadline = DateTime.UtcNow.Add(DebugDeadline);

		var options = context.Options.WithDeadline(deadline);

		return new ClientInterceptorContext<TRequest, TResponse>(
			context.Method,
			context.Host,
			options);
	}
}
