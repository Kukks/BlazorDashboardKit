using System.Security.Claims;
using BlazorDashboardKit.Models;

namespace BlazorDashboardKit.Abstractions;

public interface IWidgetAccessControl
{
    Task<bool> IsAllowedAsync(WidgetDescriptor descriptor, ClaimsPrincipal? user, CancellationToken ct = default);
}

public sealed class AllowAllWidgetAccessControl : IWidgetAccessControl
{
    public Task<bool> IsAllowedAsync(WidgetDescriptor descriptor, ClaimsPrincipal? user, CancellationToken ct = default)
        => Task.FromResult(true);
}
