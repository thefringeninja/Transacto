using Microsoft.Extensions.Logging;
using Transacto.Messages;
using Transacto.Testing;

namespace Transacto;

public interface ISpecificationWriter {
	public Task FlushAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

	IDisposable BeginSpecificationGroup(string name);

	/// <summary>
	///     Writes the specified test specification.
	/// </summary>
	/// <param name="specification">The test specification to write.</param>
	void WriteSpecification(string name, EventCentricTestSpecification specification);

	/// <summary>
	///     Writes the specified test specification.
	/// </summary>
	/// <param name="specification">The test specification to write.</param>
	void WriteSpecification(string name, ExceptionCentricTestSpecification specification);
}

public class LoggerSpecificationWriter(ILogger inner) : ISpecificationWriter {
	private IDisposable? _specificationScope;
	public IDisposable BeginSpecificationGroup(string name) {
		var scope = inner.BeginScope("{specification}", name) ?? new DisposableAction(() => { });
		inner.LogInformation(name);
		return scope;
	}

	public void WriteSpecificationGroup(string name) {
		_specificationScope?.Dispose();
		_specificationScope = inner.BeginScope("{specificationGroup}", name);
		inner.LogInformation(name);
	}

	/// <summary>
	///     Writes the specified test specification.
	/// </summary>
	/// <param name="specification">The test specification to write.</param>
	public void WriteSpecification(string name, EventCentricTestSpecification specification) {
		using var scope = inner.BeginScope("{specification}", name);
		WriteGivens(specification);
		WriteWhen(specification);
		WriteThens(specification);
	}

	/// <summary>
	///     Writes the specified test specification.
	/// </summary>
	/// <param name="specification">The test specification to write.</param>
	public void WriteSpecification(string name, ExceptionCentricTestSpecification specification) {
		using var scope = inner.BeginScope("{specification}", name);
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
		inner.LogWarning("  [{exception}] {message}", specification.Throws.GetType().Name,
			specification.Throws.Message);
	}

	private void WriteMessage(object message) {
		if (message is IFormattableMessage m) {
			var formattableString = m.ToFormattableString();
			inner.LogInformation("  " + formattableString.Format, formattableString.GetArguments());
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

internal class DisposableAction(Action dispose) : IDisposable {
	public void Dispose() => dispose();
}
