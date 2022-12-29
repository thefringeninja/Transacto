namespace Transacto.Framework;

public class CheckpointTests {
	[AutoFixtureData]
	public void Equality(Checkpoint sut) {
		var copy = new Checkpoint(sut.Memory);
		Assert.Equal(sut, copy);
	}

	[AutoFixtureData]
	public void EqualityOperator(Checkpoint sut) {
		var copy = new Checkpoint(sut.Memory);
		Assert.True(sut == copy);
	}

	[AutoFixtureData]
	public void InequalityOperator(Checkpoint left, Checkpoint right) {
		Assert.True(left != right);
	}
}
