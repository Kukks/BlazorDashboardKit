namespace BlazorDashboardKit.Models;

public class WidgetDescriptor
{
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public Type ComponentType { get; set; } = null!;

    /// <summary>
    /// Optional Blazor component that edits this widget's configuration. When set
    /// it is rendered in the config panel and should derive
    /// <c>WidgetConfigComponent&lt;TConfig&gt;</c>. When null the widget has no
    /// configuration UI (the config panel shows a "no configuration" message).
    /// </summary>
    public Type? ConfigComponentType { get; set; }

    public int MinColumnSize { get; set; } = 2;
    public int MaxColumnSize { get; set; } = 12;
    public int DefaultColumnSize { get; set; } = 6;
    public int MinRowSpan { get; set; } = 1;
    public int MaxRowSpan { get; set; } = 4;
    public int DefaultRowSpan { get; set; } = 2;
    /// <summary>Opaque permission tokens; interpreted only by IWidgetAccessControl.</summary>
    public string[] RequiredPermissions { get; set; } = [];
    public bool AllowMultiple { get; set; } = true;
    public bool RequiresConfiguration { get; set; }
    public bool AlwaysInteractive { get; set; }
    public string? IconCssClass { get; set; }
    public string? CssClass { get; set; }
}
