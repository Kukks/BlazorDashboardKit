using System.Text.Json;
using BlazorDashboardKit.Models;
using Xunit;

namespace BlazorDashboardKit.Tests.Models;

public class DashboardCollectionJsonTests
{
    [Fact]
    public void RoundTrips_Collection_WithActiveId_AndWidgets()
    {
        var c = new DashboardCollection { ActiveDashboardId = "d1" };
        c.Dashboards.Add(new DashboardDefinition { Id = "d1", Name = "Main",
            Widgets = { new WidgetPlacement { WidgetType = "Notes" } } });
        var back = JsonSerializer.Deserialize<DashboardCollection>(JsonSerializer.Serialize(c))!;
        Assert.Equal("d1", back.ActiveDashboardId);
        Assert.Single(back.Dashboards);
        Assert.Equal("Notes", back.Dashboards[0].Widgets[0].WidgetType);
    }
}
