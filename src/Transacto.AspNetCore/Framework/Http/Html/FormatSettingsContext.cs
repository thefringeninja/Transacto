using System.Text.Json.Serialization;

namespace Transacto.Framework.Http.Html;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(Formatter))]
public partial class FormatSettingsContext : JsonSerializerContext;
