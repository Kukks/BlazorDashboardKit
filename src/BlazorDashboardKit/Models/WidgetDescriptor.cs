namespace BlazorDashboardKit.Models;

public enum ConfigFieldType { Text, Textarea, Number, Select, Checkbox, Hidden }

public class ConfigFieldSchema
{
    public string Label { get; set; } = string.Empty;
    public ConfigFieldType FieldType { get; set; } = ConfigFieldType.Text;
    public List<(string Value, string Label)> Options { get; set; } = new();
    public int? Min { get; set; }
    public int? Max { get; set; }
    public int? Rows { get; set; }
}

public class WidgetDescriptor
{
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public Type ComponentType { get; set; } = null!;
    public int MinColumnSize { get; set; } = 2;
    public int MaxColumnSize { get; set; } = 12;
    public int DefaultColumnSize { get; set; } = 6;
    public int MinRowSpan { get; set; } = 1;
    public int MaxRowSpan { get; set; } = 4;
    public int DefaultRowSpan { get; set; } = 2;
    /// <summary>Opaque permission tokens; interpreted only by IWidgetAccessControl.</summary>
    public string[] RequiredPermissions { get; set; } = Array.Empty<string>();
    public bool AllowMultiple { get; set; } = true;
    public bool RequiresConfiguration { get; set; }
    public bool AlwaysInteractive { get; set; }
    public string? IconCssClass { get; set; }
    public string? CssClass { get; set; }
    public Dictionary<string, ConfigFieldSchema> ConfigSchema { get; set; } = new();
}
