using BlazorDashboardKit.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorDashboardKit.Tests.Components;

/// <summary>
/// Component-type override for the widget edit header. Implements the seam
/// contract: a single <c>[Parameter] WidgetHeaderContext Context</c>. Renders a
/// recognizable marker and a button wired to the context's Remove callback so
/// tests can prove the override replaced the default and its actions flow.
/// </summary>
public sealed class TestHeaderComponent : ComponentBase
{
    [Parameter] public WidgetHeaderContext Context { get; set; } = null!;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "custom-header");
        builder.AddContent(2, $"custom:{Context.Descriptor.Name}");
        builder.OpenElement(3, "button");
        builder.AddAttribute(4, "class", "custom-remove");
        builder.AddAttribute(5, "onclick", Context.Remove);
        builder.AddContent(6, "x");
        builder.CloseElement();
        builder.CloseElement();
    }
}
