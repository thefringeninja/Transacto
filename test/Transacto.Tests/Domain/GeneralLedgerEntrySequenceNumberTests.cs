using System;
using Xunit;

namespace Transacto.Domain {
	public class GeneralLedgerEntrySequenceNumberTests {
		[Theory, AutoTransactoData]
		public void Equality(GeneralLedgerEntrySequenceNumber sut) {
			var copy = sut;
			Assert.Equal(sut, copy);
		}

		[Theory, AutoTransactoData]
		public void EqualityOperator(GeneralLedgerEntrySequenceNumber sut) {
			var copy = sut;
			Assert.True(sut == copy);
		}

		[Theory, AutoTransactoData]
		public void InequalityOperator(GeneralLedgerEntrySequenceNumber left, GeneralLedgerEntrySequenceNumber right) {
			Assert.True(left != right);
		}

		[Theory, AutoTransactoData]
		public void SequenceNumberLessThanZeroThrows(int value) {
			var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
				new GeneralLedgerEntrySequenceNumber(-Math.Abs(value)));
			Assert.Equal(nameof(value), ex.ParamName);
		}

		[Fact]
		public void SequenceNumberZeroThrows() {
			const int value = 0;
			var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
				new GeneralLedgerEntrySequenceNumber(value));
			Assert.Equal(nameof(value), ex.ParamName);
		}

		[Theory, AutoTransactoData]
		public void ToInt32ReturnsExpectedResult(int value) {
			var sut = new GeneralLedgerEntrySequenceNumber(value);

			Assert.Equal(value, sut.ToInt32());
		}

		[Theory, AutoTransactoData]
		public void ToStringReturnsExpectedResult(int value) {
			var sut = new GeneralLedgerEntrySequenceNumber(value);

			Assert.Equal(value.ToString(), sut.ToString());
		}
	}
}
