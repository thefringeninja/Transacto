using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Html;

namespace Transacto;

public static class FormattableStringHtmlExtensions {
	/// <summary>
	/// Turn a FormattableString into HTML where each formatted argument is wrapped in a span
	/// with a color chosen by its runtime type.
	/// </summary>
	public static IHtmlContent Colorize(this FormattableString fs, CultureInfo? culture = null) {
		ArgumentNullException.ThrowIfNull(fs);
		culture ??= CultureInfo.InvariantCulture;

		var format = fs.Format;
		var args = fs.GetArguments();

		var sb = new StringBuilder();
		var i = 0;
		while (i < format.Length) {
			var c = format[i];

			switch (c) {
				// Handle escaped braces '{{' or '}}'
				case '{' when i + 1 < format.Length && format[i + 1] == '{':
					sb.Append('{');
					i += 2;
					continue;
				// parse a placeholder {index[:formatString]}
				case '{': {
					var start = i + 1;
					var end = format.IndexOf('}', start);
					if (end < 0)
						throw new FormatException("Input string was not in a correct format: missing '}'.");

					var inside = format.Substring(start, end - start); // e.g. "0", "1:yyyy-MM-dd"
					// split on first colon
					var colon = inside.IndexOf(':');
					var indexPart = colon >= 0 ? inside[..colon] : inside;
					var formatPart = colon >= 0 ? inside[(colon + 1)..] : null;

					if (!int.TryParse(indexPart, NumberStyles.None, CultureInfo.InvariantCulture, out var argIndex))
						throw new FormatException($"Invalid format item index: '{indexPart}'.");

					if (argIndex < 0 || argIndex >= args.Length)
						throw new FormatException($"Format item index {argIndex} is out of range.");

					var arg = args[argIndex];

					// Render argument with its format (if any)
					string rendered;
					if (arg is IFormattable formattable) {
						rendered = formattable.ToString(formatPart, culture);
					} else {
						// If formatPart exists but arg isn't IFormattable, ignore it and call ToString()
						rendered = arg?.ToString() ?? string.Empty;
					}

					// Choose color by runtime type (customize this mapping)
					var cssClass = Classify(arg);

					// HTML-encode the rendered piece to avoid XSS; use HtmlEncoder via HtmlString to keep it safe
					// We are in a server-side context that supports IHtmlContent; create literal-encoded value.
					var encoded = System.Net.WebUtility.HtmlEncode(rendered);
					sb.Append($"""<span class="{cssClass}">{encoded}</span>""");

					i = end + 1;
					continue;
				}
				// handle escaped '}}' -> single '}'
				case '}' when i + 1 < format.Length && format[i + 1] == '}':
					sb.Append('}');
					i += 2;
					continue;
				case '}':
					throw new FormatException("Input string was not in a correct format: '}' must be escaped as '}}'.");
				default:
					// normal literal character
					sb.Append(c);
					i++;
					break;
			}
		}

		// Return as HtmlString so Razor renders the spans as HTML
		return new HtmlString(sb.ToString());

		static string Classify(object? value) {
			const string @null = nameof(@null);
			const string number = nameof(number);
			const string @bool = nameof(@bool);
			const string @string = nameof(@string);
			const string dateTime = nameof(dateTime);
			const string scalar = nameof(scalar);
			const string other = nameof(other);

			return value switch {
				null => @null,
				byte or sbyte or short or int or uint or long or ulong => number,
				decimal or double or float => scalar,
				string => @string,
				DateTime or DateTimeOffset or DateOnly or TimeOnly => dateTime,
				Guid or TimeSpan or Uri => scalar,
				bool => @bool,
				_ => value.GetType() switch {
					{ } t when typeof(ReadOnlySpan<char>).IsAssignableTo(t) => @string,
					{ IsEnum: true } => @scalar,
					_ => other
				}
			};
		}
	}
}
