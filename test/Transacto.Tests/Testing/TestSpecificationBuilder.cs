namespace Transacto.Testing;

internal class TestSpecificationBuilder : IScenarioGivenStateBuilder, IScenarioGivenNoneStateBuilder,
	IScenarioWhenStateBuilder, IScenarioThenStateBuilder, IScenarioThenNoneStateBuilder,
	IScenarioThrowStateBuilder {
	private readonly TestSpecificationBuilderContext _context;

	public TestSpecificationBuilder() {
		_context = new TestSpecificationBuilderContext();
	}

	private TestSpecificationBuilder(TestSpecificationBuilderContext context) {
		_context = context;
	}

	public IScenarioGivenStateBuilder Given(params Fact[] facts) {
		ArgumentNullException.ThrowIfNull(facts);
		return new TestSpecificationBuilder(_context.AppendGivens(facts));
	}

	public IScenarioGivenStateBuilder Given(string identifier, params object[] events) {
		ArgumentNullException.ThrowIfNull(identifier);
		ArgumentNullException.ThrowIfNull(events);
		return
			new TestSpecificationBuilder(
				_context.AppendGivens(events.Select(@event => new Fact(identifier, @event))));
	}

	public IScenarioWhenStateBuilder When(object message) {
		ArgumentNullException.ThrowIfNull(message);
		return new TestSpecificationBuilder(_context.SetWhen(message));
	}

	ExceptionCentricTestSpecification IExceptionCentricTestSpecificationBuilder.Build() {
		return _context.ToExceptionCentricSpecification();
	}

	public IScenarioThenStateBuilder Then(params Fact[] facts) {
		ArgumentNullException.ThrowIfNull(facts);
		return new TestSpecificationBuilder(_context.AppendThens(facts));
	}

	public IScenarioThenStateBuilder Then(string identifier, params object[] events) {
		ArgumentNullException.ThrowIfNull(identifier);
		ArgumentNullException.ThrowIfNull(events);
		return
			new TestSpecificationBuilder(
				_context.AppendThens(events.Select(@event => new Fact(identifier, @event))));
	}

	public IScenarioThenNoneStateBuilder ThenNone() {
		return new TestSpecificationBuilder(_context);
	}

	public IScenarioThrowStateBuilder Throws(Exception exception) {
		ArgumentNullException.ThrowIfNull(exception);
		return new TestSpecificationBuilder(_context.SetThrows(exception));
	}

	EventCentricTestSpecification IEventCentricTestSpecificationBuilder.Build() {
		return _context.ToEventCentricSpecification();
	}

	public IScenarioGivenNoneStateBuilder GivenNone() {
		return new TestSpecificationBuilder(_context);
	}
}
