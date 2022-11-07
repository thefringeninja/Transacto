namespace Transacto.Domain; 

public class GeneralLedgerEntryIdentifierTests {
	[AutoFixtureData]
	public void Equality(Guid value) {
		var sut = new GeneralLedgerEntryIdentifier(value);
		var copy = new GeneralLedgerEntryIdentifier(value);
		Assert.Equal(sut, copy);
	}

	[AutoFixtureData]
	public void EqualityOperator(Guid value) {
		var sut = new GeneralLedgerEntryIdentifier(value);
		var copy = new GeneralLedgerEntryIdentifier(value);
		Assert.True(sut == copy);
	}

	[AutoFixtureData]
	public void InequalityOperator(GeneralLedgerEntryIdentifier left, GeneralLedgerEntryIdentifier right) {
		Assert.True(left != right);
	}

		public void EmptyValueThrows() {
		Assert.Throws<ArgumentOutOfRangeException>(() => new GeneralLedgerEntryIdentifier(Guid.Empty));
	}

	[AutoFixtureData]
	public void ToGuidReturnsExpectedResult(Guid value) {
		var sut = new GeneralLedgerEntryIdentifier(value);
		Assert.Equal(value, sut.ToGuid());
	}

	[AutoFixtureData]
	public void ToStringReturnsExpectedResult(Guid value) {
		var sut = new GeneralLedgerEntryIdentifier(value);
		Assert.Equal(value.ToString("n"), sut.ToString());
	}
}
