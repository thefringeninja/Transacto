using EventStore.Client;
using Transacto.Framework.CommandHandling;
using Xunit;

namespace Transacto.Framework {
	public class UnitOfWorkTests {
		[Fact]
		public void AccessingCurrentWhenNotStartedThrows() {
			Assert.Throws<UnitOfWorkNotStartedException>(() => UnitOfWork.Current);
		}

		[Fact]
		public void CanStart() {
			using var _ = UnitOfWork.Start(new EventStoreClient(), MessageTypeMapper.Create(),
				TransactoSerializerOptions.Events);
			Assert.False(UnitOfWork.Current.HasChanges);
		}

		[Fact]
		public void AttachingAnAggregate() {
			var aggregate = new TestAggregate();

			using var _ = UnitOfWork.Start(new EventStoreClient(), MessageTypeMapper.Create(),
				TransactoSerializerOptions.Events);
			UnitOfWork.Current.Attach("stream", aggregate, Optional<long>.Empty);
			Assert.True(UnitOfWork.Current.TryGet("stream", out var result));
			Assert.Same(aggregate, result);
		}

		[Fact]
		public void GettingChanges() {
			var aggregate = new TestAggregate();

			using var _ = UnitOfWork.Start(new EventStoreClient(), MessageTypeMapper.Create(),
				TransactoSerializerOptions.Events);
			UnitOfWork.Current.Attach("stream", aggregate, Optional<long>.Empty);
			aggregate.DoSomething();
			Assert.True(UnitOfWork.Current.HasChanges);
			Assert.Single(UnitOfWork.Current.GetChanges());
		}

		private class TestAggregate : AggregateRoot {
			public override string Id { get; } = nameof(TestAggregate);

			public void DoSomething() => Apply(new object());
		}
	}
}
