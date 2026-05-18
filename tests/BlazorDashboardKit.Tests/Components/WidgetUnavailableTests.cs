using BlazorDashboardKit.Models;
using Bunit;
using Xunit;

namespace BlazorDashboardKit.Tests.Components;

public class WidgetUnavailableTests : TestContext
{
    [Fact]
    public void Shows_Unavailable_Marker_With_Type()
    {
        var cut = RenderComponent<BlazorDashboardKit.Components.WidgetUnavailable>(p => p
            .Add(x => x.Placement, new WidgetPlacement { WidgetType = "Ghost" }));
        Assert.Contains("widget-unavailable", cut.Markup);
        Assert.Contains("Ghost", cut.Markup);
    }
}
