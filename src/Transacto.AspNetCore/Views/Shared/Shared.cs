using Microsoft.AspNetCore.Components;

namespace Transacto.Views.Shared;

internal static class Shared;

partial class Layout;

public abstract class ComponentBase<TModel> : ComponentBase {
	[Parameter]
	public required TModel Model { get; set; }
}
