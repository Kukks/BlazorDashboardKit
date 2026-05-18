namespace BlazorDashboardKit.Models;

public class DashboardCollection
{
    public string? ActiveDashboardId { get; set; }
    public List<DashboardDefinition> Dashboards { get; set; } = new();
}
