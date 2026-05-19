using System;
using BlazorDashboardKit.Models;
using Microsoft.AspNetCore.Components;

namespace BlazorDashboardKit.Components;

/// <summary>Context for an empty-dashboard override (no widgets on the active dashboard).</summary>
public sealed record EmptyDashboardContext(bool EditMode);

/// <summary>
/// Context for a custom "add widget" picker. <see cref="Available"/> is already
/// filtered by <c>IWidgetAccessControl</c>; invoke <see cref="Add"/> with a
/// descriptor to add that widget.
/// </summary>
public sealed record WidgetPickerContext(
    System.Collections.Generic.IReadOnlyList<WidgetDescriptor> Available,
    EventCallback<WidgetDescriptor> Add);

/// <summary>
/// Context for a "widget type not registered" override. The placement still
/// exists in storage; <see cref="Remove"/> deletes it.
/// </summary>
public sealed record WidgetUnavailableContext(
    WidgetPlacement Placement,
    bool EditMode,
    EventCallback Remove);

/// <summary>
/// Context for a widget-error override (the widget component threw and was
/// caught by the kit's error boundary). <see cref="Recover"/> resets the
/// boundary and re-renders the widget.
/// </summary>
public sealed record WidgetErrorContext(
    Exception Exception,
    WidgetPlacement Placement,
    WidgetDescriptor Descriptor,
    EventCallback Recover);
