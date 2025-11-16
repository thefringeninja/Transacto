namespace Transacto.Domain;

public class GeneralLedgerEntrySequenceNumberTests : ValueObjectTests<GeneralLedgerEntrySequenceNumber> {
	[AutoFixtureData]
	public void SequenceNumberLessThanZeroThrows(int value) {
		var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
			new GeneralLedgerEntrySequenceNumber(-Math.Abs(value)));
		Assert.Equal(nameof(value), ex.ParamName);
	}

	public void SequenceNumberZeroThrows() {
		const int value = 0;
		var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
			new GeneralLedgerEntrySequenceNumber(value));
		Assert.Equal(nameof(value), ex.ParamName);
	}

	[AutoFixtureData]
	public void ToInt32ReturnsExpectedResult(int value) {
		var sut = new GeneralLedgerEntrySequenceNumber(value);

		Assert.Equal(value, sut.ToInt32());
	}

	[AutoFixtureData]
	public void ToStringReturnsExpectedResult(int value) {
		var sut = new GeneralLedgerEntrySequenceNumber(value);

		Assert.Equal(value.ToString(), sut.ToString());
	}
}
