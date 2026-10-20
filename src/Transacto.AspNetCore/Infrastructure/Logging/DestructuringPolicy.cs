using System.Diagnostics.CodeAnalysis;
using Serilog.Core;
using Serilog.Events;
using Transacto.Framework.Logging;

namespace Transacto.Infrastructure.Logging;

public class DestructuringPolicy : IDestructuringPolicy {
	public bool TryDestructure(object value, ILogEventPropertyValueFactory propertyValueFactory,
		[NotNullWhen(true)] out LogEventPropertyValue? result) {
		switch (value) {
			case Scopes.Plugins scope:
				result = new StructureValue([
					new LogEventProperty(nameof(Scopes.Plugins.Plugin), new ScalarValue(scope.Plugin))
				]);
				return true;
			case Scopes.ProcessManagers scope:
				result = new StructureValue([
					new LogEventProperty(nameof(Scopes.ProcessManagers.Plugin), new ScalarValue(scope.Plugin)),
					new LogEventProperty(nameof(Scopes.ProcessManagers.CheckpointStreamName),
						new ScalarValue(scope.CheckpointStreamName)),
					new LogEventProperty(nameof(Scopes.ProcessManagers.ProcessManager),
						new ScalarValue(scope.ProcessManager))
				]);
				return true;
			case Scopes.Projections scope:
				result = new StructureValue([
					new LogEventProperty(nameof(Scopes.Projections.Projection), new ScalarValue(scope.Projection))
				]);
				return true;
			case Scopes.EventRecord scope:
				result = new StructureValue([
					new LogEventProperty(nameof(Scopes.EventRecord.StreamName), new ScalarValue(scope.StreamName)),
					new LogEventProperty(nameof(Scopes.EventRecord.ContentType),
						new ScalarValue(scope.ContentType)),
					new LogEventProperty(nameof(Scopes.EventRecord.Created), new ScalarValue(scope.Created)),
					new LogEventProperty(nameof(Scopes.EventRecord.Position), new ScalarValue(scope.Position)),
					new LogEventProperty(nameof(Scopes.EventRecord.StreamPosition),
						new ScalarValue(scope.StreamPosition)),
					new LogEventProperty(nameof(Scopes.EventRecord.Type), new ScalarValue(scope.Type)),
				]);
				return true;

			default:
				result = null;
				return false;
		}
	}
}
