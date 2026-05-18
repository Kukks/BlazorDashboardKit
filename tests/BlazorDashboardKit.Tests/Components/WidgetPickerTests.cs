using BlazorDashboardKit.Models;
using Bunit;
using Xunit;

namespace BlazorDashboardKit.Tests.Components;

public class WidgetPickerTests : TestContext
{
    [Fact]
    public void Lists_Provided_Descriptors()
    {
        var cut = RenderComponent<BlazorDashboardKit.Components.WidgetPicker>(p => p
            .Add(x => x.Available, new List<WidgetDescriptor>
                { new() { Type = "Notes", Name = "Notes", Category = "Utility" } }));
        Assert.Contains("Notes", cut.Markup);
    }
}
