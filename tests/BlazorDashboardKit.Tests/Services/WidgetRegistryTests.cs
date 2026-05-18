using BlazorDashboardKit.Models;
using BlazorDashboardKit.Services;
using Xunit;

namespace BlazorDashboardKit.Tests.Services;

public class WidgetRegistryTests
{
    private static WidgetDescriptor D(string t) => new() { Type = t, Name = t };

    [Fact]
    public void Duplicate_Type_Throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => new WidgetRegistry(new[] { D("Notes"), D("Notes") }));
        Assert.Contains("Notes", ex.Message);
    }

    [Fact]
    public void GetDescriptor_FindsByOrdinalType_OrNull()
    {
        var r = new WidgetRegistry(new[] { D("Notes") });
        Assert.Equal("Notes", r.GetDescriptor("Notes")!.Type);
        Assert.Null(r.GetDescriptor("notes"));
        Assert.Null(r.GetDescriptor("Missing"));
    }
}
