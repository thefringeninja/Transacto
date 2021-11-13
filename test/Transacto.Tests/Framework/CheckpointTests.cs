using Xunit;

namespace Transacto.Framework; 

public class CheckpointTests {
	[Theory, AutoTransactoData]
	public void Equality(Checkpoint sut) {
		var copy = new Checkpoint(sut.Memory);
		Assert.Equal(sut, copy);
	}

	[Theory, AutoTransactoData]
	public void EqualityOperator(Checkpoint sut) {
		var copy = new Checkpoint(sut.Memory);
		Assert.True(sut == copy);
	}

	[Theory, AutoTransactoData]
	public void InequalityOperator(Checkpoint left, Checkpoint right) {
		Assert.True(left != right);
	}
}