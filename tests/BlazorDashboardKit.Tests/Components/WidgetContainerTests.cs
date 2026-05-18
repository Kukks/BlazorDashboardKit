using BlazorDashboardKit.Abstractions;
using BlazorDashboardKit.Models;
using BlazorDashboardKit.Services;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BlazorDashboardKit.Tests.Components;

public class WidgetContainerTests : TestContext
{
    private WidgetDescriptor Probe(bool alwaysInteractive) => new()
    {
        Type = "Probe",
        Name = "Probe",
        Category = "Test",
        ComponentType = typeof(ReadonlyProbeWidget),
        AlwaysInteractive = alwaysInteractive
    };

    private IRenderedComponent<BlazorDashboardKit.Components.WidgetContainer> Render(
        WidgetDescriptor descriptor, bool editMode, bool readOnly, bool hostInteractive)
    {
        Services.AddSingleton<IWidgetAccessControl, AllowAllWidgetAccessControl>();
        return RenderComponent<BlazorDashboardKit.Components.WidgetContainer>(p => p
            .Add(x => x.Placement, new WidgetPlacement { WidgetType = "Probe" })
            .Add(x => x.Descriptor, descriptor)
            .Add(x => x.EditMode, editMode)
            .Add(x => x.ReadOnly, readOnly)
            .Add(x => x.HostInteractive, hostInteractive));
    }

    [Fact]
    public void Remove_Requires_A_Two_Step_Confirm()
    {
        Services.AddSingleton<IWidgetAccessControl, AllowAllWidgetAccessControl>();
        var removed = 0;
        var cut = RenderComponent<BlazorDashboardKit.Components.WidgetContainer>(p => p
            .Add(x => x.Placement, new WidgetPlacement { WidgetType = "Probe" })
            .Add(x => x.Descriptor, Probe(alwaysInteractive: false))
            .Add(x => x.EditMode, true)
            .Add(x => x.OnRemove, () => removed++));

        // First click only arms the confirm — nothing removed.
        cut.Find("button[title='Remove widget']").Click();
        Assert.Equal(0, removed);
        Assert.NotEmpty(cut.FindAll("button[title='Confirm remove']"));

        // Cancel disarms — still nothing removed, back to the plain Remove button.
        cut.Find("button[title='Cancel remove']").Click();
        Assert.Equal(0, removed);
        Assert.Empty(cut.FindAll("button[title='Confirm remove']"));
        Assert.NotEmpty(cut.FindAll("button[title='Remove widget']"));

        // Arm again then confirm — now it removes exactly once.
        cut.Find("button[title='Remove widget']").Click();
        cut.Find("button[title='Confirm remove']").Click();
        Assert.Equal(1, removed);
    }

    [Fact]
    public void AlwaysInteractive_Widget_Is_Live_When_Host_Interactive_And_Not_ReadOnly()
    {
        var cut = Render(Probe(alwaysInteractive: true), editMode: false, readOnly: false, hostInteractive: true);
        Assert.Contains("RW", cut.Find(".ro-probe").TextContent);
    }

    [Fact]
    public void Host_ReadOnly_Forces_Widget_ReadOnly_Even_If_AlwaysInteractive()
    {
        var cut = Render(Probe(alwaysInteractive: true), editMode: false, readOnly: true, hostInteractive: true);
        Assert.Contains("RO", cut.Find(".ro-probe").TextContent);
    }

    [Fact]
    public void Non_Interactive_Host_Forces_Widget_ReadOnly_Even_If_AlwaysInteractive()
    {
        // Static SSR / prerender: nothing is interactive, so an AlwaysInteractive
        // widget must still render read-only (its handlers would never wire up).
        var cut = Render(Probe(alwaysInteractive: true), editMode: true, readOnly: false, hostInteractive: false);
        Assert.Contains("RO", cut.Find(".ro-probe").TextContent);
    }
}
