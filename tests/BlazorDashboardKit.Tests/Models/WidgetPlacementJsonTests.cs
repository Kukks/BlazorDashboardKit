using System.Text.Json;
using BlazorDashboardKit.Models;
using Xunit;

namespace BlazorDashboardKit.Tests.Models;

public class WidgetPlacementJsonTests
{
    [Fact]
    public void RoundTrips_NullAndZero_OffsetRow_Distinctly()
    {
        var src = new WidgetPlacement { WidgetType = "Notes", Offset = 0, Row = null, ColumnSize = 6 };
        var json = JsonSerializer.Serialize(src);
        var back = JsonSerializer.Deserialize<WidgetPlacement>(json)!;
        Assert.Equal(0, back.Offset);
        Assert.Null(back.Row);
        Assert.Equal("Notes", back.WidgetType);
    }

    [Fact]
    public void RoundTrips_Locked()
    {
        var back = JsonSerializer.Deserialize<WidgetPlacement>(
            JsonSerializer.Serialize(new WidgetPlacement { WidgetType = "X", Locked = true }))!;
        Assert.True(back.Locked);

        var def = JsonSerializer.Deserialize<WidgetPlacement>(
            JsonSerializer.Serialize(new WidgetPlacement { WidgetType = "X" }))!;
        Assert.False(def.Locked);
    }
}
