using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc;
using Transacto.Framework.Http;

namespace Transacto.Framework.Projections;

public static class ProjectionsMiddleware {
	public static void UseProjectionStatus(this IApplicationBuilder app) {
		app.Map("/projections", inner => inner.UseRouting().UseEndpoints(ep => {
			ep.MapGet("",
				(HttpContext context, [FromServices] IEnumerable<IProjectionHostStatus> projectionHostStatus) => {
					if (context.Request.Headers.Accept.Contains("text/event-stream")) {
						return Results.ObservableServerSentEvents(
							projectionHostStatus.ObserveAll(context.RequestAborted));
					}

					return Results.Hal(projectionHostStatus
						.SelectMany(x => x.State)
						.ToDictionary(x => x.Name), Checkpoint.None, new ProjectionStateRepresentation());
				});
			ep.MapGet("{name}",
				(string name, HttpContext context,
					[FromServices] IEnumerable<IProjectionHostStatus> projectionHostStatus) => {
					if (context.Request.Headers.Accept.Contains("text/event-stream")) {
						return Results.ObservableServerSentEvents(
							projectionHostStatus.Observe(name, context.RequestAborted));
					}

					return Results.Hal(projectionHostStatus
						.SelectMany(x => x.State)
						.Where(x => x.Name == name)
						.ToDictionary(x => x.Name), Checkpoint.None, new ProjectionStateRepresentation());
				});
		}));
	}

	extension(ConcurrentDictionary<string, ProjectionState> dictionary) {
		public void UpdateAll<TState>(Func<ProjectionState, TState, ProjectionState> updater, TState state) {
			foreach (var key in dictionary.Keys) {
				dictionary.AddOrUpdate(key,
					static (key, x) => x.updater(ProjectionState.Named(key), x.state),
					static (_, value, state) => state.updater(value, state.state),
					(updater, state));
			}
		}

		public void UpdateAll(Func<ProjectionState, ProjectionState> updater) {
			foreach (var key in dictionary.Keys) {
				dictionary.AddOrUpdate(key,
					static (key, updater) => updater(ProjectionState.Named(key)),
					static (_, value, updater) => updater(value),
					updater);
			}
		}
	}


	extension(IEnumerable<IProjectionHostStatus> items) {
		private IObservable<ProjectionState> ObserveAll(CancellationToken ct) => items.EnumerateAll(ct).ToObservable()
			.GroupBy(item => item.Name).SelectMany(Observable.DistinctUntilChanged);

		private async IAsyncEnumerable<ProjectionState> EnumerateAll([EnumeratorCancellation] CancellationToken ct) {
			var array = items as IProjectionHostStatus[] ?? items.ToArray();

			using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

			Loop:
			try {
				await timer.WaitForNextTickAsync(ct);
			} catch (OperationCanceledException) {
				yield break;
			}

			foreach (var item in array) {
				foreach (var state in item.State) {
					if (ct.IsCancellationRequested) {
						yield break;
					}

					yield return state;
				}
			}

			goto Loop;
		}

		private IObservable<ProjectionState> Observe(string name, CancellationToken ct) =>
			items.EnumerateAll(name, ct).ToObservable().DistinctUntilChanged();

		private async IAsyncEnumerable<ProjectionState> EnumerateAll(
			string name, [EnumeratorCancellation] CancellationToken ct) {
			using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

			Loop:
			try {
				await timer.WaitForNextTickAsync(ct);
			} catch (OperationCanceledException) {
				yield break;
			}

			foreach (var item in items) {
				if (ct.IsCancellationRequested) {
					yield break;
				}

				if (!item.TryGetState(name, out var state)) {
					continue;
				}

				yield return state;
			}

			goto Loop;
		}
	}
}
