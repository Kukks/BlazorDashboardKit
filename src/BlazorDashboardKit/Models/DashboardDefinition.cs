namespace BlazorDashboardKit.Models;

public class DashboardDefinition
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "Dashboard";
    public string? Description { get; set; }
    public bool IsDefault { get; set; }
    public List<WidgetPlacement> Widgets { get; set; } = new();
}
