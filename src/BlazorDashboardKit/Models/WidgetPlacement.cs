using System.Text.Json.Nodes;

namespace BlazorDashboardKit.Models;

public class WidgetPlacement
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string WidgetType { get; set; } = string.Empty;
    /// <summary>Logical/auto-flow ordering hint. When Row is set, Row is authoritative.</summary>
    public int Order { get; set; }
    public int ColumnSize { get; set; } = 6;
    public int RowSpan { get; set; } = 2;
    /// <summary>Explicit gridstack column (x); null = auto-flow. Nullable so column 0 round-trips.</summary>
    public int? Offset { get; set; }
    /// <summary>Explicit gridstack row (y); null = auto-flow. Preserves real layouts with gaps.</summary>
    public int? Row { get; set; }
    /// <summary>When true the widget cannot be moved or resized (pinned in place).</summary>
    public bool Locked { get; set; }
    public JsonObject? Config { get; set; }
}
