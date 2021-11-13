﻿using System.Collections.Generic;

namespace Transacto.Testing;

/// <summary>
///     Contract to compare if the expected and actual event are equal.
/// </summary>
public interface IEventComparer {
	/// <summary>
	///     Compares the expected to the actual event.
	/// </summary>
	/// <param name="expected">The expected event.</param>
	/// <param name="actual">The actual event.</param>
	/// <returns>An enumeration of <see cref="EventComparisonDifference">differences</see>, or empty if none found.</returns>
	IEnumerable<EventComparisonDifference> Compare(object expected, object actual);
}
