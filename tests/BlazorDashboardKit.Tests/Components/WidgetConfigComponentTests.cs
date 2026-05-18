using System.Text.Json.Nodes;
using Bunit;
using Xunit;

namespace BlazorDashboardKit.Tests.Components;

public class WidgetConfigComponentTests : TestContext
{
    [Fact]
    public void Model_Is_Built_From_ConfigJson()
    {
        var cut = RenderComponent<TestConfigEditor>(p => p
            .Add(x => x.ConfigJson, new JsonObject { ["Name"] = "seed" }));

        Assert.Equal("seed", cut.Find("input.test-config-name").GetAttribute("value"));
    }

    [Fact]
    public void In_Progress_Edits_Survive_A_Re_Render_With_Same_Config()
    {
        // The base rebuilds Model only when a *different* ConfigJson instance
        // arrives (panel open), not on every render — otherwise each keystroke's
        // re-render would discard what the user just typed.
        JsonObject? emitted = null;
        var cut = RenderComponent<TestConfigEditor>(p => p
            .Add(x => x.ConfigJson, new JsonObject { ["Name"] = "old" })
            .Add(x => x.ConfigJsonChanged, (JsonObject c) => emitted = c));

        cut.Find("input.test-config-name").Change("typed");
        Assert.Equal("typed", emitted!["Name"]!.GetValue<string>());

        // Force a re-render without changing the ConfigJson reference.
        cut.Render();

        Assert.Equal("typed", cut.Find("input.test-config-name").GetAttribute("value"));
    }
}
