using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Components;

namespace BlazorDashboardKit.Components;

/// <summary>
/// Base for a widget's configuration UI. A widget points its
/// <c>WidgetDescriptor.ConfigComponentType</c> at a component deriving this;
/// the dashboard's config panel renders it, supplies the current config as a
/// working copy, and persists it when the user clicks Save.
/// </summary>
/// <remarks>
/// Derived components bind inputs to <see cref="Model"/> and call
/// <see cref="NotifyChangedAsync"/> after a change. The panel owns Save/Cancel
/// semantics: <see cref="Model"/> is already an isolated working copy, so edits
/// never touch the stored config until Save.
/// </remarks>
public abstract class WidgetConfigComponent<TConfig> : ComponentBase
    where TConfig : class, new()
{
    private JsonObject? _lastConfigJson;

    /// <summary>The working config the panel handed in (its deep clone).</summary>
    [Parameter] public JsonObject? ConfigJson { get; set; }

    /// <summary>Raised with the re-serialized config whenever the model changes.</summary>
    [Parameter] public EventCallback<JsonObject> ConfigJsonChanged { get; set; }

    /// <summary>The typed, editable configuration model.</summary>
    protected TConfig Model { get; private set; } = new();

    protected override void OnParametersSet()
    {
        // Rebuild the model only when the panel hands in a different config
        // instance (panel opens), not on every render — otherwise in-progress
        // edits would be discarded on each keystroke-triggered re-render.
        if (!ReferenceEquals(_lastConfigJson, ConfigJson))
        {
            _lastConfigJson = ConfigJson;
            Model = ConfigJson is null
                ? new TConfig()
                : ConfigJson.Deserialize<TConfig>() ?? new TConfig();
        }
    }

    /// <summary>
    /// Serialize the current <see cref="Model"/> and push it back to the panel.
    /// Call this after mutating <see cref="Model"/> from an input event.
    /// </summary>
    protected Task NotifyChangedAsync()
    {
        var json = JsonSerializer.SerializeToNode(Model)!.AsObject();
        return ConfigJsonChanged.InvokeAsync(json);
    }
}
