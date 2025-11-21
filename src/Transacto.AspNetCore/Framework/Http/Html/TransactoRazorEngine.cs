using System.Collections.Concurrent;
using System.Collections.Immutable;
using Hallo;
using Microsoft.AspNetCore.Html;
using NodaTime;
using RazorEngineCore;

namespace Transacto.Framework.Http.Html;

public class TransactoRazorEngine {
	public class View<T> : RazorEngineTemplateBase<T> {
		public string? Layout { get; set; }
		internal Func<string, object?, string> IncludeCallback { get; set; } = null!;
		internal Func<string> RenderBodyCallback { get; set; } = null!;
		public string Include(string key, object? model = null) => IncludeCallback(key, model);
		public dynamic ViewBag { get; set; } = new object();

		public string RenderBody() => RenderBodyCallback();

		public ITagHelper Tags { get; set; } = null!;
	}

	private readonly ConcurrentDictionary<Type, object> _templates = [];
	private readonly RazorEngine _engine = new();

	public IRazorEngineCompiledTemplate<View<TModel>> Compile<TModel>() => Compile<TModel, TModel>();

	public IRazorEngineCompiledTemplate<View<TModel>> Compile<TKey, TModel>() =>
		(IRazorEngineCompiledTemplate<View<TModel>>)_templates.GetOrAdd(typeof(TKey), static (type, engine) => {
			using var reader = new StreamReader(
				type.Assembly.GetManifestResourceStream(type, $"{type.Name}.cshtml") ??
				throw new ArgumentException($"Embedded resource key {type} not found, BUG!!", nameof(type)));

			var content = reader.ReadToEnd();

			return engine.Compile<View<TModel>>(content, builder => {
				builder.AddAssemblyReference(typeof(ImmutableArray).Assembly);
				builder.AddAssemblyReference(typeof(LocalDate).Assembly);
				builder.AddAssemblyReference(typeof(Link).Assembly);
				builder.AddAssemblyReference(typeof(IHtmlContent).Assembly);

				builder.AddUsing(typeof(object).Namespace!);
				builder.AddUsing(typeof(AsyncEnumerator).Namespace!);
				builder.AddUsing(typeof(Enumerable).Namespace!);
				builder.AddUsing(typeof(Link).Namespace!);
				builder.AddUsing(typeof(RazorEngine).Namespace!
				);
			});
		}, _engine);
}
