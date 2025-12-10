using System.Collections;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Transacto;

internal partial class PluginServiceCollection(ILogger logger) : IServiceCollection {
	private readonly ServiceCollection _inner = [];

	[ExcludeFromCodeCoverage]
	public IEnumerator<ServiceDescriptor> GetEnumerator() => _inner.GetEnumerator();

	[ExcludeFromCodeCoverage]
	IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_inner).GetEnumerator();

	public void Add(ServiceDescriptor serviceDescriptor) {
		LogAddition(logger, serviceDescriptor);
		_inner.Add(serviceDescriptor);
	}

	[ExcludeFromCodeCoverage]
	public void Clear() {
		LogCleared(logger);
		_inner.Clear();
	}

	[ExcludeFromCodeCoverage]
	public bool Contains(ServiceDescriptor item) => _inner.Contains(item);

	public void CopyTo(ServiceDescriptor[] array, int arrayIndex) => _inner.CopyTo(array, arrayIndex);

	[ExcludeFromCodeCoverage]
	public bool Remove(ServiceDescriptor serviceDescriptor) {
		LogRemoval(logger, serviceDescriptor);
		return _inner.Remove(serviceDescriptor);
	}

	public int Count => _inner.Count;
	[ExcludeFromCodeCoverage] public bool IsReadOnly => _inner.IsReadOnly;

	[ExcludeFromCodeCoverage]
	public int IndexOf(ServiceDescriptor item) => _inner.IndexOf(item);

	[ExcludeFromCodeCoverage]
	public void Insert(int index, ServiceDescriptor item) {
		LogInsertion(logger, item, index);
		_inner.Insert(index, item);
	}

	[ExcludeFromCodeCoverage]
	public void RemoveAt(int index) {
		LogRemovingIndex(logger, index);
		_inner.RemoveAt(index);
	}

	public ServiceDescriptor this[int index] {
		get => _inner[index];
		[ExcludeFromCodeCoverage] set => _inner[index] = value;
	}

	[LoggerMessage(LogLevel.Trace, "Adding {serviceDescriptor}")]
	static partial void LogAddition(ILogger logger, ServiceDescriptor serviceDescriptor);

	[LoggerMessage(LogLevel.Trace, "Cleared")]
	static partial void LogCleared(ILogger logger);

	[LoggerMessage(LogLevel.Trace, "Removing {serviceDescriptor}")]
	static partial void LogRemoval(ILogger logger, ServiceDescriptor serviceDescriptor);

	[LoggerMessage(LogLevel.Trace, "Inserting {serviceDescriptor} at {index}")]
	static partial void LogInsertion(ILogger logger, ServiceDescriptor serviceDescriptor, int index);

	[LoggerMessage(LogLevel.Trace, "Removing {index}")]
	static partial void LogRemovingIndex(ILogger logger, int index);
}
