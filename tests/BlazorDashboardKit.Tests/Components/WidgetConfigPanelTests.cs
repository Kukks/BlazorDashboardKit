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

    [Fact]
    public void Save_Emits_Edited_Clone_Without_Mutating_Original_Config()
    {
        var schema = new Dictionary<string, ConfigFieldSchema>
            { ["Title"] = new() { Label = "Title", FieldType = ConfigFieldType.Text } };
        var original = new JsonObject { ["Title"] = "old" };
        JsonObject? emitted = null;

        var cut = RenderComponent<BlazorDashboardKit.Components.WidgetConfigPanel>(p => p
            .Add(x => x.Visible, true)
            .Add(x => x.Schema, schema)
            .Add(x => x.Config, original)
            .Add(x => x.ConfigChanged, (JsonObject c) => emitted = c));

        cut.Find("input").Change("new");
        cut.Find(".btn-primary").Click();

        // (a) emitted clone reflects the edit
        Assert.NotNull(emitted);
        Assert.Equal("new", emitted!["Title"]!.GetValue<string>());

        // (b) the original instance passed in is untouched (deep-clone isolation)
        Assert.Equal("old", original["Title"]!.GetValue<string>());
    }
}
