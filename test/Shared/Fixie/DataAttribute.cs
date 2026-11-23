using Fixie;

namespace Transacto;

public abstract class DataAttribute : Attribute {
	public abstract IEnumerable<object[]> GetParameters(Test test);
}
