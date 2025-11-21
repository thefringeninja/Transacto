using System.Reflection;
using Fixie;

namespace Transacto;

[AttributeUsage(AttributeTargets.Method)]
internal class MemberDataAttribute(string name) : DataAttribute {
	public string Name { get; } = name;

	public override IEnumerable<object[]> GetParameters(Test test) =>
		(test.Method.DeclaringType?.GetMethod(Name,
				BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
			?.Invoke(null, null) as IEnumerable<object[]>)!;
}