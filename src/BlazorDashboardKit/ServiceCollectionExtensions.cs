using BlazorDashboardKit.Abstractions;
using BlazorDashboardKit.Models;
using BlazorDashboardKit.Services;
using BlazorDashboardKit.Stores;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BlazorDashboardKit;

public sealed class BlazorDashboardOptions
{
    internal Func<IServiceProvider, IDashboardStore>? StoreFactory { get; private set; }

    public void UseJsonFileStore(string rootPath)
        => StoreFactory = _ => new JsonFileDashboardStore(rootPath);
}

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBlazorDashboard(
        this IServiceCollection services, Action<BlazorDashboardOptions>? configure = null)
    {
        var opts = new BlazorDashboardOptions();
        configure?.Invoke(opts);

        if (opts.StoreFactory is not null)
            services.TryAddSingleton(opts.StoreFactory);
        else
            services.TryAddSingleton<IDashboardStore, InMemoryDashboardStore>();

        services.TryAddSingleton<IWidgetAccessControl, AllowAllWidgetAccessControl>();
        services.TryAddScoped<DashboardService>();
        services.TryAddScoped<DashboardJsInterop>();
        services.TryAddSingleton<WidgetRegistry>();
        return services;
    }

    public static IServiceCollection AddDashboardWidget<TComponent>(
        this IServiceCollection services, WidgetDescriptor descriptor)
        where TComponent : IComponent
    {
        descriptor.ComponentType = typeof(TComponent);
        services.AddSingleton(descriptor);
        return services;
    }
}
