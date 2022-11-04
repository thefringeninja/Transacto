﻿using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;

namespace Transacto.Testing;

/// <summary>
///     Writes test specifications to a <see cref="TextWriter" /> for human consumption.
/// </summary>
public class TestSpecificationTextWriter : IEventCentricTestSpecificationWriter,
	IExceptionCentricTestSpecificationWriter {
	private readonly IndentedTextWriter _writer;

	/// <summary>
	///     Initializes a new instance of <see cref="TestSpecificationTextWriter" />.
	/// </summary>
	/// <param name="writer">The text writer to write on.</param>
	public TestSpecificationTextWriter(TextWriter writer) {
		if (writer == null) throw new ArgumentNullException(nameof(writer));
		_writer = new IndentedTextWriter(writer, "  ");
	}

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

		_writer.WriteLine("Given");
		WriteFacts(specification.Givens);
	}

	private void WriteWhen(EventCentricTestSpecification specification) {
		_writer.WriteLine("When");
		WriteMessage(specification.When);
	}

	private void WriteThens(EventCentricTestSpecification specification) {
		_writer.WriteLine("Then");
		WriteFacts(specification.Thens);
	}

	private void WriteGivens(ExceptionCentricTestSpecification specification) {
		if (specification.Givens.Length == 0) return;

		_writer.WriteLine("Given");
		WriteFacts(specification.Givens);
	}

	private void WriteWhen(ExceptionCentricTestSpecification specification) {
		_writer.WriteLine("When");
		WriteMessage(specification.When);
	}

	private void WriteThrows(ExceptionCentricTestSpecification specification) {
		_writer.WriteLine("Throws");
		_writer.Indent++;
		_writer.WriteLine("[{0}] {1}", specification.Throws.GetType().Name, specification.Throws.Message);
		_writer.Indent--;
	}

	private void WriteMessage(object message) {
		_writer.Indent++;
		_writer.WriteLine(message);
		_writer.Indent--;
	}

	private void WriteFacts(IEnumerable<Fact> facts) {
		_writer.Indent++;
		using (var enumerator = facts.GetEnumerator()) {
			var moved = enumerator.MoveNext();
			if (moved) {
				_writer.Write(enumerator.Current.Event);
				moved = enumerator.MoveNext();
				while (moved) {
					_writer.WriteLine(",");
					_writer.Write(enumerator.Current.Event);
					moved = enumerator.MoveNext();
				}

				_writer.WriteLine();
			} else {
				_writer.WriteLine("nothing happened");
			}
		}

		_writer.Indent--;
	}
}
