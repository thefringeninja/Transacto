namespace Transacto.Framework.ProcessManagers;

public class ProcessManagerEventResolveTests {
	public void ZeroRegistrationsDoesNotThrow() {
		var sut = ProcessManagerEventResolve.WhenEqualToHandlerMessageType(
			Array.Empty<MessageHandler<Checkpoint>>());

		sut.Invoke(new object());
	}

	public void MultipleRegistrationsThrow() {
		var sut = ProcessManagerEventResolve.WhenEqualToHandlerMessageType(new[] {
			new MessageHandler<Checkpoint>(typeof(object), (_, _) => new ValueTask<Checkpoint>(Checkpoint.None)),
			new MessageHandler<Checkpoint>(typeof(object), (_, _) => new ValueTask<Checkpoint>(Checkpoint.None))
		});

		var ex = Assert.Throws<ProcessManagerEventResolveException>(() => sut.Invoke(new object()));

		Assert.Equal(typeof(object), ex.EventType);
		Assert.Equal(2, ex.HandlerCount);
	}

	public void SingleRegistrationReturnsExpectedResult() {
		var handler =
			new MessageHandler<Checkpoint>(typeof(object), (_, _) => new ValueTask<Checkpoint>(Checkpoint.None));

		var sut = ProcessManagerEventResolve.WhenEqualToHandlerMessageType(new[] { handler });
		Assert.Equal(handler, sut.Invoke(new object()));
	}
}
