using BlazorDashboardKit.Components;
using Microsoft.AspNetCore.Components;

namespace BlazorDashboardKit.Tests.Components;

/// <summary>
/// Minimal widget used only by <see cref="BaseWidgetComponentTests"/>. It derives
/// <see cref="BaseWidgetComponent{TConfig}"/> and renders a body element whose markup
/// contains the literal token "test-widget-body".
/// </summary>
public sealed class TestWidget : BaseWidgetComponent<TestWidget.TestWidgetConfig>
{
    public sealed class TestWidgetConfig
    {
        public string Value { get; set; } = string.Empty;
    }

    protected override RenderFragment? ChildContent => builder =>
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "test-widget-body");
        builder.AddContent(2, "body");
        builder.CloseElement();
    };
}
