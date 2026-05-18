using BlazorDashboardKit.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorDashboardKit.Tests.Components;

/// <summary>
/// Test widget that renders its resolved <c>Readonly</c> state ("RO"/"RW") so
/// tests can assert how the host computes read-only for a widget.
/// </summary>
public sealed class ReadonlyProbeWidget : BaseWidgetComponent<ReadonlyProbeWidget.Cfg>
{
    public sealed class Cfg { }

    protected override RenderFragment? ChildContent => builder =>
    {
        builder.OpenElement(0, "span");
        builder.AddAttribute(1, "class", "ro-probe");
        builder.AddContent(2, Readonly ? "RO" : "RW");
        builder.CloseElement();
    };
}
