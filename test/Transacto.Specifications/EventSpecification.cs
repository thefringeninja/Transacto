using Transacto.Testing;

namespace Transacto;

public abstract class EventSpecification(IEventCentricTestSpecificationBuilder builder) {
	private readonly EventCentricTestSpecification _spec = builder.Build();

	public static implicit operator EventCentricTestSpecification(EventSpecification spec) => spec._spec;
}

public abstract class ExceptionSpecification(IExceptionCentricTestSpecificationBuilder builder) {
	private readonly ExceptionCentricTestSpecification _spec = builder.Build();

	public static implicit operator ExceptionCentricTestSpecification(ExceptionSpecification spec) => spec._spec;
}
