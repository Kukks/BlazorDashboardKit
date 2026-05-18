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

    [Fact]
    public void Clicking_Item_Invokes_OnWidgetAdded_With_Descriptor()
    {
        WidgetDescriptor? added = null;
        var descriptor = new WidgetDescriptor { Type = "Notes", Name = "Notes", Category = "Utility" };

        var cut = RenderComponent<BlazorDashboardKit.Components.WidgetPicker>(p => p
            .Add(x => x.Available, new List<WidgetDescriptor> { descriptor })
            .Add(x => x.OnWidgetAdded, (WidgetDescriptor d) => added = d));

        cut.Find("button.dropdown-item").Click();

        Assert.NotNull(added);
        Assert.Same(descriptor, added);
        Assert.Equal("Notes", added!.Type);
    }
}
