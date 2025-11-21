using Fixie;

namespace Transacto;

internal abstract class DataAttribute : Attribute {
	public abstract IEnumerable<object[]> GetParameters(Test test);
}