using Xunit;

namespace Transacto.Framework; 

public class UnitOfWorkTests {
		public void AccessingCurrentWhenNotStartedThrows() {
		Assert.Throws<UnitOfWorkNotStartedException>(() => UnitOfWork.Current);
	}

		public void CanStart() {
		using var _ = UnitOfWork.Start();
		Assert.False(UnitOfWork.Current.HasChanges);
	}

		public void AttachingAnAggregate() {
		var aggregate = new TestAggregate();

		using var _ = UnitOfWork.Start();
		UnitOfWork.Current.Attach(new("stream", aggregate));
		Assert.True(UnitOfWork.Current.TryGet("stream", out var result));
		Assert.Same(aggregate, result);
	}

		public void AttachingAnAggregateWhenStreamNameExistsThrows() {
		using var _ = UnitOfWork.Start();
		UnitOfWork.Current.Attach(new("stream", new TestAggregate()));
		Assert.Throws<ArgumentException>(() => UnitOfWork.Current.Attach(new("stream", new TestAggregate())));
	}

		public void GettingChanges() {
		var aggregate = new TestAggregate();

		using var _ = UnitOfWork.Start();
		UnitOfWork.Current.Attach(new ("stream", aggregate, Optional<long>.Empty));
		aggregate.DoSomething();
		Assert.True(UnitOfWork.Current.HasChanges);
		Assert.Single(UnitOfWork.Current.GetChanges());
	}

	private class TestAggregate : AggregateRoot {
		public override string Id { get; } = nameof(TestAggregate);

		public void DoSomething() => Apply(new object());

		protected override void ApplyEvent(object e) {
		}
	}
}
