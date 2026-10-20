using Microsoft.AspNetCore.Html;
using RazorEngineCore;
using Transacto.Messages;
using Transacto.Testing;

namespace Transacto;

public readonly record struct SpecificationGroupMarker;

public class HtmlSpecificationWriter(TextWriter writer) : ISpecificationWriter {
	public class Template : RazorEngineTemplateBase<IEnumerable<(string, IList<(string, object)>)>>;

	private readonly RazorEngine _razorEngine = new();

	private string? _currentSpecificationGroup;

	private readonly Dictionary<string, IList<(string, object)>> _specifications = [];

	public IDisposable BeginSpecificationGroup(string name) {
		_currentSpecificationGroup = name;
		_specifications[name] = [];
		return new DisposableAction(() => _currentSpecificationGroup = null);
	}

	private void AddSpecification(string name, object specification) {
		_specifications[_currentSpecificationGroup ?? throw new Exception("No specification group")]
			.Add((name, specification));
	}

	public void WriteSpecification(string name, EventCentricTestSpecification specification) =>
		AddSpecification(name, specification);

	public void WriteSpecification(string name, ExceptionCentricTestSpecification specification) =>
		AddSpecification(name, specification);

	public async Task FlushAsync(CancellationToken cancellationToken = default) {
		var template = await GetTemplateAsync(cancellationToken);

		var compiledTemplate = await _razorEngine.CompileAsync<Template>(
			template,
			options => {
				options.AddAssemblyReference(typeof(IFormattableMessage).Assembly);
				options.AddAssemblyReference(typeof(ValueTuple<>).Assembly);
				options.AddAssemblyReference(typeof(IHtmlContent).Assembly);
				options.AddAssemblyReference(typeof(IList<>).Assembly);
			},
			cancellationToken);

		var output = await compiledTemplate.RunAsync(template => 
			template.Model = _specifications.Select(pair => (pair.Key, pair.Value)));

		await writer.WriteAsync(output);
		await writer.FlushAsync(cancellationToken);
		_specifications.Clear();
	}

	private static async Task<string> GetTemplateAsync(CancellationToken cancellationToken) {
		using var reader = new StreamReader(
			typeof(ScenarioFixture).Assembly.GetManifestResourceStream(typeof(ScenarioFixture),
				"HtmlSpecifications.cshtml") ?? throw new Exception("BUG!!1"));
		return await reader.ReadToEndAsync(cancellationToken);
	}
}
