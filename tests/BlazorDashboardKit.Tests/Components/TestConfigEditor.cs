using BlazorDashboardKit.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorDashboardKit.Tests.Components;

/// <summary>
/// Minimal config editor used by <see cref="WidgetConfigPanelTests"/>. Derives
/// <see cref="WidgetConfigComponent{TConfig}"/> and renders a single text input
/// bound to <c>Model.Name</c>, calling <c>NotifyChangedAsync</c> on change.
/// </summary>
public sealed class TestConfigEditor : WidgetConfigComponent<TestConfigEditor.TestConfig>
{
    public sealed class TestConfig
    {
        public string Name { get; set; } = string.Empty;
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "input");
        builder.AddAttribute(1, "class", "test-config-name");
        builder.AddAttribute(2, "value", Model.Name);
        builder.AddAttribute(3, "onchange", EventCallback.Factory.Create<ChangeEventArgs>(
            this, async e =>
            {
                Model.Name = e.Value?.ToString() ?? string.Empty;
                await NotifyChangedAsync();
            }));
        builder.CloseElement();
    }
}
