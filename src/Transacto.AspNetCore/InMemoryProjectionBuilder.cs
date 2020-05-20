using System;
using Projac;

namespace Transacto {
	public class InMemoryProjectionBuilder {
		private readonly AnonymousProjectionBuilder<InMemoryReadModel> _inner;

		public InMemoryProjectionBuilder() : this(new AnonymousProjectionBuilder<InMemoryReadModel>()) {
		}

		private InMemoryProjectionBuilder(AnonymousProjectionBuilder<InMemoryReadModel> inner) {
			_inner = inner;
		}

		public InMemoryProjectionBuilder When<T>(Action<InMemoryReadModel, Envelope<T>> handler) =>
			new InMemoryProjectionBuilder(_inner.When(handler));

		public AnonymousProjection<InMemoryReadModel> Build() => _inner.Build();
	}
}
