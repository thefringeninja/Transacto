using System.IO;
using System.Text;
using Xunit.Abstractions;

#nullable enable
namespace Transacto {
	internal class TestOutputHelperTextWriter : TextWriter {
		private readonly ITestOutputHelper _output;
		public override Encoding Encoding { get; } = Encoding.UTF8;

		public TestOutputHelperTextWriter(ITestOutputHelper output) {
			_output = output;
		}

		public override void Write(string? value) => _output.WriteLine(value?.Trim());
	}
}
