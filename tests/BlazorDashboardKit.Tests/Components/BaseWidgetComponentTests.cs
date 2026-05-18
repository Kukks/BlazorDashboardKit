using System.Security.Claims;
using BlazorDashboardKit.Abstractions;
using BlazorDashboardKit.Models;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BlazorDashboardKit.Tests.Components;

public class BaseWidgetComponentTests : TestContext
{
    private sealed class ThrowingAccess : IWidgetAccessControl
    {
        public Task<bool> IsAllowedAsync(WidgetDescriptor d, ClaimsPrincipal? u, CancellationToken ct = default)
            => throw new InvalidOperationException("boom");
    }

    [Fact]
    public void Denies_Render_When_AccessControl_Throws()
    {
        Services.AddSingleton<IWidgetAccessControl>(new ThrowingAccess());
        var cut = RenderComponent<TestWidget>(p => p
            .Add(x => x.RequiredPermissions, new[] { "perm" }));
        Assert.Contains("widget-access-denied", cut.Markup);
    }

    [Fact]
    public void Renders_Body_When_No_Permissions_Required()
    {
        Services.AddSingleton<IWidgetAccessControl>(new AllowAllWidgetAccessControl());
        var cut = RenderComponent<TestWidget>();
        Assert.Contains("test-widget-body", cut.Markup);
    }

    [Fact]
    public void Renders_Body_When_Access_Granted()
    {
        Services.AddSingleton<IWidgetAccessControl>(new AllowAllWidgetAccessControl());
        var cut = RenderComponent<TestWidget>(p => p
            .Add(x => x.RequiredPermissions, new[] { "perm" }));
        Assert.Contains("test-widget-body", cut.Markup);
    }
}
