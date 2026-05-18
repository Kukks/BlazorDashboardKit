using System.Text.Json.Nodes;
using BlazorDashboardKit.Models;
using Bunit;
using Xunit;

namespace BlazorDashboardKit.Tests.Components;

public class WidgetConfigPanelTests : TestContext
{
    [Fact]
    public void Renders_Text_Field_From_Schema()
    {
        var schema = new Dictionary<string, ConfigFieldSchema>
            { ["Title"] = new() { Label = "Title", FieldType = ConfigFieldType.Text } };
        var cut = RenderComponent<BlazorDashboardKit.Components.WidgetConfigPanel>(p => p
            .Add(x => x.Visible, true)
            .Add(x => x.Schema, schema)
            .Add(x => x.Config, new JsonObject()));
        Assert.Contains("Title", cut.Markup);
        Assert.Contains("input", cut.Markup);
    }
}
