using Xunit;

namespace Transacto.Framework.CommandHandling; 

public class CommandHandlerResolveTests {
		public void ZeroRegistrationsThrows() {
		var sut = CommandResolve.WhenEqualToHandlerMessageType(Array.Empty<MessageHandler<Checkpoint>>());

		var ex = Assert.Throws<CommandResolveException>(() => sut.Invoke(new object()));

		Assert.Equal(typeof(object), ex.CommandType);
		Assert.Equal(0, ex.HandlerCount);
	}

		public void MultipleRegistrationsThrow() {
		var sut = CommandResolve.WhenEqualToHandlerMessageType(new[] {
			new MessageHandler<Checkpoint>(typeof(object), (_, _) => new ValueTask<Checkpoint>(Checkpoint.None)),
			new MessageHandler<Checkpoint>(typeof(object), (_, _) => new ValueTask<Checkpoint>(Checkpoint.None))
		});

		var ex = Assert.Throws<CommandResolveException>(() => sut.Invoke(new object()));

		Assert.Equal(typeof(object), ex.CommandType);
		Assert.Equal(2, ex.HandlerCount);
	}

		public void SingleRegistrationReturnsExpectedResult() {
		var handler =
			new MessageHandler<Checkpoint>(typeof(object), (_, _) => new ValueTask<Checkpoint>(Checkpoint.None));

		var sut = CommandResolve.WhenEqualToHandlerMessageType(new[] {handler});
		Assert.Equal(handler, sut.Invoke(new object()));
	}
}
