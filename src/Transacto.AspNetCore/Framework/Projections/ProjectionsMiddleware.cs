using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc;
using Transacto.Framework.Http;
using Transacto.Framework.Http.Html;

namespace Transacto.Framework.Projections;

public static class ProjectionsMiddleware {
	public static void UseProjectionStatus(this IApplicationBuilder app) {
		app.Map("/projections", inner => inner.UseRouting().UseEndpoints(ep => {
			ep.MapGet("",
				(HttpContext context, [FromServices] ProjectionsController projectionsController) => {
					if (context.Request.Headers.Accept.Contains("text/event-stream")) {
						return Results.ServerSentEvents(projectionsController.ObserveAll(context.RequestAborted));
					}

					return Results.Hal(projectionsController, Checkpoint.None,
						new ProjectionsControllerRepresentation());
				});
			ep.MapGet("{name}",
				(string name, HttpContext context,
					[FromServices] ProjectionsController projectionsController) => {
					if (context.Request.Headers.Accept.Contains("text/event-stream")) {
						return Results.ObservableServerSentEvents(
							projectionsController.Observe(name, context.RequestAborted));
					}

					return Results.Hal(projectionsController.State[name], Checkpoint.None,
						new ProjectionStateRepresentation());
				});
			ep.MapDelete("{name}", async (string name, 
				[FromServices] ProjectionsController projectionsController,
				[FromServices] SiteRoot siteRoot) => {
				await projectionsController.Delete(name);

				return Results.Redirect($"{siteRoot.Uri.AbsolutePath}/projections");
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
}

public class ProjectionsController(IEnumerable<IProjectionsControl> projections) {
	public Dictionary<string, ProjectionState> State => projections
		.SelectMany(x => x.State)
		.ToDictionary(x => x.Name);


	public async IAsyncEnumerable<ProjectionState> ObserveAll([EnumeratorCancellation] CancellationToken ct) {
		var array = projections as IProjectionHostStatus[] ?? projections.ToArray();

		var cache = new Dictionary<string, ProjectionState>();
		using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(300));

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

				if (!cache.TryGetValue(state.Name, out var current) || current != state ) {
					yield return state;
					
				}

				cache[state.Name] = state;
			}
		}

		goto Loop;
	}

	public IObservable<ProjectionState> Observe(string name, CancellationToken ct) =>
		EnumerateNamed(name, ct).ToObservable().DistinctUntilChanged();

	private async IAsyncEnumerable<ProjectionState> EnumerateNamed(
		string name, [EnumeratorCancellation] CancellationToken ct) {
		using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(300));

		Loop:
		try {
			await timer.WaitForNextTickAsync(ct);
		} catch (OperationCanceledException) {
			yield break;
		}

		ProjectionState current = default;
		foreach (var item in projections) {
			if (ct.IsCancellationRequested) {
				yield break;
			}

			if (!item.TryGetState(name, out var state)) {
				continue;
			}

			if (current != state) {

				yield return state;
			}

			current = state;
		}

		goto Loop;
	}

	public async Task Delete(string name) {
		foreach (var projection in projections) {
			if (projection.State.All(state => state.Name != name)) {
				continue;
			}

			await projection.Delete(name);
			return;
		}

		throw new InvalidOperationException($"Could not find a projection named {name}");
	}
}

public interface IProjectionsControl : IProjectionHostStatus {
	public Task Delete(string name);
}
