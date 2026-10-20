using System.Text.Json;
using System.Text.Json.Schema;
using System.Text.Json.Serialization;
using Transacto.Framework;
using Transacto.Framework.Http;
using Transacto.Plugins;
using Transacto.Plugins.FinancialStatements;
using Transacto.Plugins.FinancialStatements.BalanceSheet;

if (args.Length == 0) {
	return 1;
}

_ = typeof(Standard).ToString();
JsonSerializerOptions options = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

var targetType = typeof(BalanceSheetReports);

var typeInfo = SerializerContext.Default.GetTypeInfo(targetType);

if (typeInfo is null) {
	return 1;
}

var schema = typeInfo.GetJsonSchemaAsNode();

Console.WriteLine(schema.ToJsonString(options));

return 0;


[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
	Converters = [typeof(TransactoHttpSerializerOptions.LocalDateConverter), typeof(TransactoHttpSerializerOptions.PeriodConverter)])]
[JsonSerializable(typeof(Transacto.Plugins.FinancialStatements.BalanceSheet.BalanceSheetReports))]
public sealed partial class SerializerContext : JsonSerializerContext {
}
