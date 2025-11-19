using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using RazorEngineCore;

namespace Transacto.Framework.Http.Html;

public interface ITagHelper;

internal class TagHelper : ITagHelper {
	private TagHelper() { }
	public static readonly TagHelper Instance = new();
}

public static class HtmlHelperExtensions {
	public static IHtmlContent? Span(this ITagHelper _, string content, object? htmlAttributes = null) {
		var tag = new TagBuilder("span");

		tag.InnerHtml.SetContent(content);

		if (htmlAttributes is not null) {
			var attributes = HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);
			tag.MergeAttributes(attributes);
		}

		using var writer = new StringWriter();
		tag.WriteTo(writer, HtmlEncoder.Default);
		return new HtmlString(writer.ToString());
	}

	public static IHtmlContent? FormatAmount(this ITagHelper htmlHelper, decimal amount) {
		var value = amount >= 0m
			? amount.ToString("N2")
			: $"({-amount:N2})";
		return htmlHelper.Span(value, amount < 0m ? new { style = "color: red;" } : null);
	}
}
