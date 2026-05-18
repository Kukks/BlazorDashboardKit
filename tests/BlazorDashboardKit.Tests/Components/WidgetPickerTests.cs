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
    public void Toggle_Button_Opens_And_Closes_Menu_Without_Bootstrap_Js()
    {
        // The picker must drive its own open/close state in Blazor: the sample
        // host loads Bootstrap CSS but not Bootstrap's JS bundle, so a
        // data-bs-toggle-only dropdown never opens. Clicking the toggle must
        // add/remove the Bootstrap `.show` class via Blazor, not JS.
        var cut = RenderComponent<BlazorDashboardKit.Components.WidgetPicker>(p => p
            .Add(x => x.Available, new List<WidgetDescriptor>
                { new() { Type = "Notes", Name = "Notes", Category = "Utility" } }));

        Assert.DoesNotContain("show", cut.Find(".dropdown-menu").ClassList);

        cut.Find("button.dropdown-toggle").Click();
        Assert.Contains("show", cut.Find(".dropdown-menu").ClassList);

        cut.Find("button.dropdown-toggle").Click();
        Assert.DoesNotContain("show", cut.Find(".dropdown-menu").ClassList);
    }

    [Fact]
    public void Selecting_Widget_Closes_The_Menu()
    {
        // After picking a widget the menu should collapse, matching the
        // dismiss-on-select behaviour Bootstrap's JS used to provide.
        var cut = RenderComponent<BlazorDashboardKit.Components.WidgetPicker>(p => p
            .Add(x => x.Available, new List<WidgetDescriptor>
                { new() { Type = "Notes", Name = "Notes", Category = "Utility" } })
            .Add(x => x.OnWidgetAdded, (WidgetDescriptor _) => { }));

        cut.Find("button.dropdown-toggle").Click();
        Assert.Contains("show", cut.Find(".dropdown-menu").ClassList);

        cut.Find("button.dropdown-item").Click();
        Assert.DoesNotContain("show", cut.Find(".dropdown-menu").ClassList);
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
