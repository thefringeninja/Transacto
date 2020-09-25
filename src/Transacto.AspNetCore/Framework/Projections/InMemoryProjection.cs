using System;
using Projac;

namespace Transacto.Framework.Projections {
	public abstract class InMemoryProjection<T> : Projection<InMemorySession> where T : class, IMemoryReadModel {
		protected void When<TMessage>(Action<T, TMessage> handler) => base.When<Envelope<TMessage>>(
			(target, envelope) => {
				var model = target.Get<T>();

				if (!model.HasValue) {
					return;
				}

				model.Value.Checkpoint = envelope.Position;
				handler(model.Value, envelope.Message);
			});
	}
}
