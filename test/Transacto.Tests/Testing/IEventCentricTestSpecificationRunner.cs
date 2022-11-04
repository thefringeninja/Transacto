using System;
using System.Threading.Tasks;

namespace Transacto.Testing;

/// <summary>
///     Represents an event centric test specification runner.
/// </summary>
public interface IEventCentricTestSpecificationRunner {
	/// <summary>
	///     Runs the specified test specification.
	/// </summary>
	/// <param name="specification">The test specification to run.</param>
	/// <returns>The result of running the test specification.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="specification" /> is <c>null</c>.</exception>
	Task<EventCentricTestResult> Run(EventCentricTestSpecification specification);
}
