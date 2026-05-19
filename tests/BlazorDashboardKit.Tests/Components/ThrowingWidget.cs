using BlazorDashboardKit.Components;
using Microsoft.AspNetCore.Components;

namespace BlazorDashboardKit.Tests.Components;

/// <summary>
/// Widget whose body throws on render, so the kit's <c>ErrorBoundary</c> catches
/// it and renders the (overridable) error seam. Derives the real widget base so
/// the parameters <c>WidgetContainer</c> passes are valid.
/// </summary>
public sealed class ThrowingWidget : BaseWidgetComponent<ThrowingWidget.Cfg>
{
    public sealed class Cfg { }

    protected override RenderFragment? ChildContent =>
        _ => throw new InvalidOperationException("widget boom");
}
