using Serilog.Core;
using Serilog.Events;

namespace Transacto;

internal class FixieSink(TextWriter write) : ILogEventSink {
	public void Emit(LogEvent logEvent) => write.WriteLine(logEvent.RenderMessage());
}
