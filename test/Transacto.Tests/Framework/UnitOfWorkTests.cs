using Transacto.Infrastructure.EventStore;

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
		UnitOfWork.Current.Attach(new("stream", aggregate, Expected.NoStream));
		Assert.True(UnitOfWork.Current.TryGet("stream", out var result));
		Assert.Same(aggregate, result);
	}

	public void AttachingAnAggregateWhenStreamNameExistsThrows() {
		using var _ = UnitOfWork.Start();
		UnitOfWork.Current.Attach(new("stream", new TestAggregate(), Expected.NoStream));
		Assert.Throws<ArgumentException>(() =>
			UnitOfWork.Current.Attach(new("stream", new TestAggregate(), Expected.NoStream)));
	}

	public void GettingChanges() {
		var aggregate = new TestAggregate();

		using var _ = UnitOfWork.Start();
		UnitOfWork.Current.Attach(new("stream", aggregate, Expected.NoStream));
		aggregate.DoSomething();
		Assert.True(UnitOfWork.Current.HasChanges);
		Assert.Single(UnitOfWork.Current.GetChanges());
	}

	private class TestAggregate : AggregateRoot, IAggregateRoot<TestAggregate> {
		public override string Id { get; } = nameof(TestAggregate);
		public static TestAggregate Factory() => new();

		public void DoSomething() => Apply(new object());

		protected override void ApplyEvent(object e) {
		}
	}
}
