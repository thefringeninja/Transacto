using System;
using System.Collections.Generic;
using System.Linq;

namespace Transacto.Testing;

internal class FactEqualityComparer : IEqualityComparer<Fact> {
	private readonly IFactComparer _comparer;

	public FactEqualityComparer(IFactComparer comparer) {
		if (comparer == null) throw new ArgumentNullException(nameof(comparer));
		_comparer = comparer;
	}

	public bool Equals(Fact x, Fact y) {
		return !_comparer.Compare(x, y).Any();
	}

	public int GetHashCode(Fact obj) {
		return obj.GetHashCode();
	}
}
