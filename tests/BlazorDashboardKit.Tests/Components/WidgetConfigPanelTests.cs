using System.Text.Json.Nodes;
using Bunit;
using Xunit;

namespace BlazorDashboardKit.Tests.Components;

public class WidgetConfigPanelTests : TestContext
{
    [Fact]
    public void Renders_The_Widgets_Config_Component()
    {
        var cut = RenderComponent<BlazorDashboardKit.Components.WidgetConfigPanel>(p => p
            .Add(x => x.Visible, true)
            .Add(x => x.ConfigComponentType, typeof(TestConfigEditor))
            .Add(x => x.Config, new JsonObject { ["Name"] = "hello" }));

        var input = cut.Find("input.test-config-name");
        Assert.Equal("hello", input.GetAttribute("value"));
    }

    [Fact]
    public void No_Config_Component_Shows_Message_And_No_Save()
    {
        var cut = RenderComponent<BlazorDashboardKit.Components.WidgetConfigPanel>(p => p
            .Add(x => x.Visible, true)
            .Add(x => x.ConfigComponentType, (Type?)null)
            .Add(x => x.Config, new JsonObject()));

        Assert.Contains("no configuration", cut.Markup);
        Assert.Empty(cut.FindAll(".btn-primary"));
    }

    [Fact]
    public void Save_Emits_Edited_Clone_Without_Mutating_Original_Config()
    {
        var original = new JsonObject { ["Name"] = "old" };
        JsonObject? emitted = null;

        var cut = RenderComponent<BlazorDashboardKit.Components.WidgetConfigPanel>(p => p
            .Add(x => x.Visible, true)
            .Add(x => x.ConfigComponentType, typeof(TestConfigEditor))
            .Add(x => x.Config, original)
            .Add(x => x.ConfigChanged, (JsonObject c) => emitted = c));

        cut.Find("input.test-config-name").Change("new");
        cut.Find(".btn-primary").Click();

        // (a) emitted clone reflects the edit
        Assert.NotNull(emitted);
        Assert.Equal("new", emitted!["Name"]!.GetValue<string>());

        // (b) the original instance passed in is untouched (deep-clone isolation)
        Assert.Equal("old", original["Name"]!.GetValue<string>());
    }
}
