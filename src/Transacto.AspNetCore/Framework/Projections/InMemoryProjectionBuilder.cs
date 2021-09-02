using System;
using Projac;

namespace Transacto.Framework.Projections {
	public class InMemoryProjectionBuilder<T> : AnonymousProjectionBuilder<InMemoryProjectionDatabase>
		where T : MemoryReadModel, new() {
		public AnonymousProjectionBuilder<InMemoryProjectionDatabase> When(Func<T, object, T> handler) =>
			base.When<Envelope>(
				(target, envelope) => {
					var model = target.Get<T>();

					target.Set(handler(model.HasValue ? model.Value : new T(), envelope.Message) with {
						Checkpoint = envelope.Position
					});
				});
	}
}
