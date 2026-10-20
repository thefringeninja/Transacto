using Fixie;

namespace Transacto;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
internal class InlineDataAttribute(params object[] parameters) : DataAttribute {
	public IEnumerable<object[]> Parameters { get; } = [parameters];

	public override IEnumerable<object[]> GetParameters(Test test) => Parameters;
}