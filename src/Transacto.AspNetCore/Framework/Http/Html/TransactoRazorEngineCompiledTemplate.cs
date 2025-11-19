using Microsoft.AspNetCore.Mvc.Rendering;
using RazorEngineCore;

namespace Transacto.Framework.Http.Html;

public abstract class TransactoRazorEngineCompiledTemplate {
}

public class TransactoRazorEngineCompiledTemplate<T>(
	IRazorEngineCompiledTemplate<TransactoRazorEngine.View<T>> compiledTemplate,
	Dictionary<string, IRazorEngineCompiledTemplate<TransactoRazorEngine.View<object>>> compiledParts)
	: TransactoRazorEngineCompiledTemplate where T : class {
	public string Run(T? model = null, object? viewBag = null) => RunInternal(compiledTemplate, model, viewBag);

	private string RunInternal<TModel>(
		IRazorEngineCompiledTemplate<TransactoRazorEngine.View<TModel>> compiledTemplate, TModel? model,
		object? viewBag) {
		TransactoRazorEngine.View<TModel> templateReference = null!;

		var result = compiledTemplate.Run(view => {
			view.Model = model!;
			view.ViewBag = new AnonymousTypeWrapper(viewBag);
			view.IncludeCallback = (key, model) => RunInternal(compiledParts[key], model, viewBag);
			view.Tags = TagHelper.Instance;
			templateReference = view;
		});
		if (templateReference.Layout is null) {
			return result;
		}

		return compiledParts[templateReference.Layout].Run(view => {
			view.RenderBodyCallback = () => result;
			view.ViewBag = new AnonymousTypeWrapper(viewBag);
			view.Tags = TagHelper.Instance;
			view.IncludeCallback = (key, model) => RunInternal(compiledParts[key], model, viewBag);
		});
	}
}
