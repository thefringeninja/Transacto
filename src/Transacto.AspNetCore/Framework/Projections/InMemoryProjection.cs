using System.Collections;
using KurrentDB.Client;
using Projac;

namespace Transacto.Framework.Projections;

public class InMemoryProjection<T>(Func<T, object, T> handler) : InMemoryProjection(typeof(T).Name,
	new AnonymousProjectionBuilder<InMemoryProjectionDatabase>()
		.When<Envelope>((target, envelope) => {
			var optional = target.Get<T>();
			var (message, position) = envelope;
			
			var model = optional switch{
				Some<T> some => some.Value,
				None => new T()
			};

			target.Set(handler(model, message) with {
				Position = Option<Position>.Some(position)
			});
		})
		.Build()) where T : MemoryReadModel, new() {
}

public abstract class InMemoryProjection(string name, AnonymousProjection<InMemoryProjectionDatabase> inner)
	: IEnumerable<ProjectionHandler<InMemoryProjectionDatabase>> {
	public string Name { get; } = name;
	private AnonymousProjection<InMemoryProjectionDatabase> Inner { get; } = inner;

	public static implicit operator ProjectionHandler<InMemoryProjectionDatabase>[](InMemoryProjection source) =>
		source.Inner;

	IEnumerator<ProjectionHandler<InMemoryProjectionDatabase>>
		IEnumerable<ProjectionHandler<InMemoryProjectionDatabase>>.GetEnumerator() => Inner.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Inner).GetEnumerator();
}
