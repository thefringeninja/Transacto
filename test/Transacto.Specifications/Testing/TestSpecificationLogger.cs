using Microsoft.Extensions.Logging;
using Transacto.Messages;

namespace Transacto.Testing;

public class TestSpecificationLogger(ILogger inner) : IEventCentricTestSpecificationWriter,
	IExceptionCentricTestSpecificationWriter {
	/// <summary>
	///     Writes the specified test specification.
	/// </summary>
	/// <param name="specification">The test specification to write.</param>
	public void Write(EventCentricTestSpecification specification) {
		WriteGivens(specification);
		WriteWhen(specification);
		WriteThens(specification);
	}

	/// <summary>
	///     Writes the specified test specification.
	/// </summary>
	/// <param name="specification">The test specification to write.</param>
	public void Write(ExceptionCentricTestSpecification specification) {
		WriteGivens(specification);
		WriteWhen(specification);
		WriteThrows(specification);
	}

	private void WriteGivens(EventCentricTestSpecification specification) {
		if (specification.Givens.Length == 0) return;

		inner.LogInformation("Given:");
		WriteFacts(specification.Givens);
	}

	private void WriteWhen(EventCentricTestSpecification specification) {
		inner.LogInformation("When:");
		WriteMessage(specification.When);
	}

	private void WriteThens(EventCentricTestSpecification specification) {
		inner.LogInformation("Then:");
		WriteFacts(specification.Thens);
	}

	private void WriteGivens(ExceptionCentricTestSpecification specification) {
		if (specification.Givens.Length == 0) return;

		inner.LogInformation("Given:");
		WriteFacts(specification.Givens);
	}

	private void WriteWhen(ExceptionCentricTestSpecification specification) {
		inner.LogInformation("When:");
		WriteMessage(specification.When);
	}

	private void WriteThrows(ExceptionCentricTestSpecification specification) {
		inner.LogInformation("Throws:");
		inner.LogWarning("  [{exception}] {message}", specification.Throws.GetType().Name, specification.Throws.Message);
	}

	private void WriteMessage(object message) {
		if (message is IMessage m) {
			var (template, args) = m.GetMessage();
			inner.LogInformation("  " + template, args);
		} else {
			inner.LogInformation("  " + message);
		}
	}

	private void WriteFacts(IEnumerable<Fact> facts) {
		using var enumerator = facts.GetEnumerator();
		var moved = enumerator.MoveNext();
		if (moved) {
			WriteMessage(enumerator.Current.Event);
			moved = enumerator.MoveNext();
			while (moved) {
				WriteMessage(enumerator.Current.Event);
				moved = enumerator.MoveNext();
			}
		} else {
			inner.LogInformation("  nothing happened");
		}
	}
}
