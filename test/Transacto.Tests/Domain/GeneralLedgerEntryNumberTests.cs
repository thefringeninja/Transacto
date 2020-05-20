using System;
using System.Collections.Generic;
using AutoFixture;
using Xunit;

namespace Transacto.Domain {
	public class GeneralLedgerEntryNumberTests {
		[Theory, AutoTransactoData]
		public void Equality(GeneralLedgerEntryNumber sut) {
			var copy = new GeneralLedgerEntryNumber(sut.Prefix, sut.SequenceNumber);
			Assert.Equal(sut, copy);
		}

		[Theory, AutoTransactoData]
		public void EqualityOperator(GeneralLedgerEntryNumber sut) {
			var copy = new GeneralLedgerEntryNumber(sut.Prefix, sut.SequenceNumber);
			Assert.True(sut == copy);
		}

		[Theory, AutoTransactoData]
		public void InequalityOperator(GeneralLedgerEntryNumber left, GeneralLedgerEntryNumber right) {
			Assert.False(left == right);
		}

		public static IEnumerable<object[]> InvalidPrefixCases() {
			var fixture = new ScenarioFixture();
			yield return new object[] {" ", fixture.Create<int>()};
			yield return new object[] {string.Empty, fixture.Create<int>()};
			yield return new object[] {" a", fixture.Create<int>()};
			yield return new object[] {"a ", fixture.Create<int>()};
			yield return new object[] {"a	", fixture.Create<int>()};
			yield return new object[]
				{new string('a', GeneralLedgerEntryNumber.MaxPrefixLength + 1), fixture.Create<int>()};
		}

		[Theory, MemberData(nameof(InvalidPrefixCases))]
		public void InvalidPrefix(string prefix, int sequenceNumber) {
			var ex = Assert.Throws<ArgumentException>(() => new GeneralLedgerEntryNumber(prefix, sequenceNumber));
			Assert.Equal("prefix", ex.ParamName);
		}

		[Theory, AutoTransactoData]
		public void SequenceNumberLessThanZeroThrows(Random random, int sequenceNumber) {
			var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new GeneralLedgerEntryNumber(
				new string('a', random.Next(1, GeneralLedgerEntryNumber.MaxPrefixLength)),
				-Math.Abs(sequenceNumber)));
			Assert.Equal("sequenceNumber", ex.ParamName);
		}

		[Theory, AutoTransactoData]
		public void SequenceNumberZeroThrows(Random random) {
			var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new GeneralLedgerEntryNumber(
				new string('a', random.Next(1, GeneralLedgerEntryNumber.MaxPrefixLength)), 0));
			Assert.Equal("sequenceNumber", ex.ParamName);
		}

		[Theory, AutoTransactoData]
		public void ParseValidValueReturnsExpectedResult(GeneralLedgerEntryNumber number) {
			var sut = GeneralLedgerEntryNumber.Parse(number.ToString());
			Assert.Equal(number, sut);
		}

		[Theory, AutoTransactoData]
		public void TryParseValidValueReturnsExpectedResult(GeneralLedgerEntryNumber number) {
			Assert.True(GeneralLedgerEntryNumber.TryParse(number.ToString(), out var sut));
			Assert.Equal(number, sut);
		}

		public static IEnumerable<object[]> ParseInvalidValueTestCases() {
			yield return new object[] {string.Empty};
			yield return new object[] {"a"};
			yield return new object[] {"a-"};
			yield return new object[] {"a--1"};
			yield return new object[] {" "};
		}

		[Theory, MemberData(nameof(ParseInvalidValueTestCases))]
		public void ParseInvalidValueReturnsExpectedResult(string value) {
			Assert.Throws<FormatException>(() => GeneralLedgerEntryNumber.Parse(value));
		}

		[Theory, MemberData(nameof(ParseInvalidValueTestCases))]
		public void TryParseInvalidValueReturnsExpectedResult(string value) {
			Assert.False(GeneralLedgerEntryNumber.TryParse(value, out var sut));
			Assert.Equal(default, sut);
		}
	}
}
