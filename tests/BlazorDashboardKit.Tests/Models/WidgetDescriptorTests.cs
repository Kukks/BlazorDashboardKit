using BlazorDashboardKit.Models;
using Xunit;

namespace BlazorDashboardKit.Tests.Models;

public class WidgetDescriptorTests
{
    [Fact]
    public void Defaults_AreSane()
    {
        var d = new WidgetDescriptor { Type = "Notes", Name = "Notes" };
        Assert.Equal(6, d.DefaultColumnSize);
        Assert.Empty(d.RequiredPermissions);
        Assert.True(d.AllowMultiple);
        Assert.NotNull(d.ConfigSchema);
    }
}
