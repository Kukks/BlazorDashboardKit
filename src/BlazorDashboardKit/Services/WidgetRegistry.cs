using System.Collections.Immutable;
using BlazorDashboardKit.Models;

namespace BlazorDashboardKit.Services;

public class WidgetRegistry
{
    public ImmutableArray<WidgetDescriptor> Descriptors { get; }

    public WidgetRegistry(IEnumerable<WidgetDescriptor> descriptors)
    {
        var arr = descriptors.ToImmutableArray();
        var dupes = arr.GroupBy(d => d.Type, StringComparer.Ordinal)
            .Where(g => g.Count() > 1).Select(g => g.Key).ToArray();
        if (dupes.Length > 0)
            throw new InvalidOperationException(
                $"Duplicate dashboard widget type(s): {string.Join(", ", dupes)}");
        Descriptors = arr;
    }

    public WidgetDescriptor? GetDescriptor(string type)
        => Descriptors.FirstOrDefault(d => string.Equals(d.Type, type, StringComparison.Ordinal));
}
