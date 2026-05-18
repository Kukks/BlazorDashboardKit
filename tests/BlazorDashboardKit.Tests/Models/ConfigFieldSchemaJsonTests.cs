using System.Text.Json;
using BlazorDashboardKit.Models;
using Xunit;

namespace BlazorDashboardKit.Tests.Models;

public class ConfigFieldSchemaJsonTests
{
    [Fact]
    public void RoundTrips_Select_Options()
    {
        var s = new ConfigFieldSchema
        {
            Label = "Period",
            FieldType = ConfigFieldType.Select,
            Options = { new SelectOption("week", "This Week"), new SelectOption("month", "This Month") }
        };
        var back = JsonSerializer.Deserialize<ConfigFieldSchema>(JsonSerializer.Serialize(s))!;
        Assert.Equal(2, back.Options.Count);
        Assert.Equal("week", back.Options[0].Value);
        Assert.Equal("This Week", back.Options[0].Label);
    }
}
