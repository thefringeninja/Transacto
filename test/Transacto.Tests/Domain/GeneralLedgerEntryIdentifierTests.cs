namespace Transacto.Domain;

public class GeneralLedgerEntryIdentifierTests : ValueObjectTests<GeneralLedgerEntryIdentifier> {
	public void EmptyValueThrows() =>
		Assert.Throws<ArgumentOutOfRangeException>(() => new GeneralLedgerEntryIdentifier(Guid.Empty));

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
