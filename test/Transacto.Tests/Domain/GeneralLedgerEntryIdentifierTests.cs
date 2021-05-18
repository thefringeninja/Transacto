using System;
using Xunit;

namespace Transacto.Domain {
	public class GeneralLedgerEntryIdentifierTests {
		[Theory, AutoTransactoData]
		public void Equality(Guid value) {
			var sut = new GeneralLedgerEntryIdentifier(value);
			var copy = new GeneralLedgerEntryIdentifier(value);
			Assert.Equal(sut, copy);
		}

		[Theory, AutoTransactoData]
		public void EqualityOperator(Guid value) {
			var sut = new GeneralLedgerEntryIdentifier(value);
			var copy = new GeneralLedgerEntryIdentifier(value);
			Assert.True(sut == copy);
		}

		[Theory, AutoTransactoData]
		public void InequalityOperator(GeneralLedgerEntryIdentifier left, GeneralLedgerEntryIdentifier right) {
			Assert.True(left != right);
		}

		[Fact]
		public void EmptyValueThrows() {
			Assert.Throws<ArgumentOutOfRangeException>(() => new GeneralLedgerEntryIdentifier(Guid.Empty));
		}

		[Theory, AutoTransactoData]
		public void ToGuidReturnsExpectedResult(Guid value) {
			var sut = new GeneralLedgerEntryIdentifier(value);
			Assert.Equal(value, sut.ToGuid());
		}

		[Theory, AutoTransactoData]
		public void ToStringReturnsExpectedResult(Guid value) {
			var sut = new GeneralLedgerEntryIdentifier(value);
			Assert.Equal(value.ToString("n"), sut.ToString());
		}
	}
}
