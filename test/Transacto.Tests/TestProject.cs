namespace Transacto;

partial class TestProject {
	partial class TestDiscovery {
		public TestDiscovery() : this(
			concreteClasses => concreteClasses
				.Where(x => x.Name.EndsWith("Tests"))
				.Shuffle(),
			publicMethods => publicMethods
				.Where(x => !x.IsStatic)
				.Where(x => !LifecycleMethods.Contains(x.Name))
				.Shuffle()) {
		}
	}

	partial class Execution : ParallelExecution;
}
