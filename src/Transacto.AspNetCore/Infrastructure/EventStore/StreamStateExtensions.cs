// ReSharper disable once CheckNamespace
namespace KurrentDB.Client;

internal static class StreamStateExtensions {
	public static StreamState Next(this StreamState value) {
		ArgumentOutOfRangeException.ThrowIfEqual(value.HasPosition, false);
		return StreamState.StreamRevision((ulong)(value.ToInt64() + 1));
	}	
}
