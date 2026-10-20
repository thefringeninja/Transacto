using Fixie;

namespace Transacto;

[AttributeUsage(AttributeTargets.Method)]
internal class ClassDataAttribute : DataAttribute {
	public IEnumerable<object[]> Parameters { get; }

	public ClassDataAttribute(Type classType) {
		if (!typeof(IEnumerable<object[]>).IsAssignableFrom(classType)) {
			throw new ArgumentException($"Expected {classType.FullName} to implement IEnumerable<object[]>.",
				nameof(classType));
		}

		if (classType.GetConstructor([]) is null) {
			throw new ArgumentException($"Expected {classType.FullName} to have an empty parameterless constructor.",
				nameof(classType));
		}

		Parameters = (IEnumerable<object[]>)Activator.CreateInstance(classType)!;
	}

	public override IEnumerable<object[]> GetParameters(Test test) => Parameters;
}