using System.Text.Json.Serialization;

namespace Transacto.Framework.Http.Html;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(FormatSettings))]
public partial class FormatSettingsContext : JsonSerializerContext;