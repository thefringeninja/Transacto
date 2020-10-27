using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

// ReSharper disable CheckNamespace
namespace Microsoft.AspNetCore.Builder {
	// ReSharper restore CheckNamespace
	public static class HttpContextExtensions {
		public static bool TryParseGuid(this HttpContext context, string key, out Guid value) {
			value = default;
			return Guid.TryParse(context.GetRouteValue(key)?.ToString(), out value);
		}

		public static bool TryParseInt32(this HttpContext context, string key, out int value) {
			value = default;
			return int.TryParse(context.GetRouteValue(key)?.ToString(), out value);
		}
	}
}
