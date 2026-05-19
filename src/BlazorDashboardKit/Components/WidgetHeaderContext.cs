using BlazorDashboardKit.Models;
using Microsoft.AspNetCore.Components;

namespace BlazorDashboardKit.Components;

/// <summary>
/// Data + actions handed to a custom widget edit-header override (either a
/// <c>RenderFragment&lt;WidgetHeaderContext&gt;</c> or a component declaring
/// <c>[Parameter] public WidgetHeaderContext Context</c>). A custom header owns
/// its own UX (including any remove-confirm) and wires its controls to these
/// callbacks; <see cref="Placement"/>.<c>Locked</c> is the current lock state.
/// </summary>
public sealed record WidgetHeaderContext(
    WidgetPlacement Placement,
    WidgetDescriptor Descriptor,
    EventCallback Configure,
    EventCallback ToggleLock,
    EventCallback Duplicate,
    EventCallback Remove);
