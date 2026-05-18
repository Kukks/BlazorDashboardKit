using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Nodes;
using BlazorDashboardKit.Abstractions;
using BlazorDashboardKit.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorDashboardKit.Components;

/// <summary>
/// Host-agnostic base for dashboard widgets. Provides typed configuration backed by a
/// <see cref="JsonObject"/>, an edit lifecycle, parameter-change tracking, and a
/// fail-closed access check delegated to <see cref="IWidgetAccessControl"/>.
/// </summary>
/// <remarks>
/// Derived widgets supply their body by overriding <see cref="ChildContent"/>. The base
/// renders that body only when access is granted; otherwise it emits an element carrying
/// the <c>widget-access-denied</c> CSS class and nothing else.
/// </remarks>
public abstract class BaseWidgetComponent<TConfig> : ComponentBase, IDisposable
    where TConfig : class, new()
{
    [Inject] private IWidgetAccessControl AccessControl { get; set; } = null!;

    [CascadingParameter] private Task<AuthenticationState>? AuthState { get; set; }

    private readonly CancellationTokenSource _cts = new();
    private CancellationTokenSource? _checkCts;

    public bool Loading
    {
        get => _loading;
        set
        {
            if (_loading == value) return;
            _loading = value;
            InvokeAsync(StateHasChanged);
        }
    }

    public virtual TConfig? TypedConfig
    {
        get => _typedConfig;
        set
        {
            _typedConfig = value;
            _cachedConfig = value is null ? null : JsonSerializer.SerializeToNode(value)!.AsObject();
            InvokeAsync(StateHasChanged);
            InvokeAsync(TypedConfigChanged);
        }
    }

    protected virtual Task TypedConfigChanged() => Task.CompletedTask;

    // The custom accessor is intentional: assigning Config rebuilds the typed config and
    // the cached JsonObject (mirrors the original BTCPay framework design and keeps the
    // content-based change tracking in SetParametersAsync correct). BL0007 only flags the
    // non-auto property shape; the behaviour is deliberate.
#pragma warning disable BL0007
    [Parameter]
    public virtual JsonObject? Config
    {
        get => _cachedConfig;
        set
        {
            if (_cachedConfig is null && value is null)
                return;
            if (_cachedConfig is not null && value is not null && JsonNode.DeepEquals(_cachedConfig, value))
                return;
            TypedConfig = value is null ? default : value.Deserialize<TConfig>();
        }
    }
#pragma warning restore BL0007

    [Parameter] public int Size { get; set; }
    [Parameter] public string StoreId { get; set; } = string.Empty;
    [Parameter] public string UserId { get; set; } = string.Empty;
    [Parameter] public bool Readonly { get; set; }
    [Parameter] public string WidgetType { get; set; } = string.Empty;
    [Parameter] public string[] RequiredPermissions { get; set; } = [];

    [Parameter] public EventCallback<JsonObject> ConfigChanged { get; set; }
    [Parameter] public EventCallback<bool> EditModeChanged { get; set; }
    [Parameter] public EventCallback OnRemove { get; set; }
    [Parameter] public EventCallback RequestConfigure { get; set; }

    // Access control
    protected bool HasAccess { get; private set; } = false;

    protected TConfig? EditConfig { get; set; }

    /// <summary>
    /// True when data-relevant parameters (StoreId, UserId, Config) have changed since the
    /// last render, meaning the widget should re-fetch its data. Widgets should check this
    /// at the top of OnParametersSetAsync and skip expensive data loading when false.
    /// </summary>
    protected bool DataParametersChanged { get; private set; }

    /// <summary>
    /// True when the Size parameter changed, meaning chart widgets should re-render their charts.
    /// </summary>
    protected bool SizeChanged { get; private set; }

    /// <summary>
    /// True only on the very first OnParametersSetAsync call (initial load).
    /// </summary>
    protected bool IsFirstLoad => !_hasLoadedOnce;

    /// <summary>
    /// The widget body. Derived widgets override this to render their content. It is only
    /// rendered when <see cref="HasAccess"/> is true.
    /// </summary>
    protected virtual RenderFragment? ChildContent => null;

    protected bool EditMode
    {
        get => _editMode;
        set
        {
            if (_editMode == value) return;
            _editMode = value;
            InvokeAsync(() => EditModeChanged.InvokeAsync(_editMode));
            InvokeAsync(StateHasChanged);
        }
    }

    private bool _editMode;
    private bool _loading;
    private TConfig? _typedConfig;
    private JsonObject? _cachedConfig;
    private bool _hasLoadedOnce;

    public override Task SetParametersAsync(ParameterView parameters)
    {
        // Snapshot previous values before Blazor applies new ones
        var oldStoreId = StoreId;
        var oldUserId = UserId;
        var oldConfig = _cachedConfig;
        var oldSize = Size;

        // Determine if data-relevant parameters changed by peeking at incoming values.
        // We check before base.SetParametersAsync because OnParametersSetAsync runs inside it.
        var newStoreId = parameters.TryGetValue<string>(nameof(StoreId), out var s) ? s : oldStoreId;
        var newUserId = parameters.TryGetValue<string>(nameof(UserId), out var u) ? u : oldUserId;
        var newSize = parameters.TryGetValue<int>(nameof(Size), out var sz) ? sz : oldSize;
        // For Config, compare by content. The TypedConfig setter rebuilds _cachedConfig
        // (SerializeToNode), so reference equality would mark every parameter pass as
        // a config change and force unnecessary reload + access checks.
        var configChanged = parameters.TryGetValue<JsonObject>(nameof(Config), out var newConfig)
            && !JsonNode.DeepEquals(newConfig, oldConfig);

        DataParametersChanged = !_hasLoadedOnce
            || oldStoreId != newStoreId
            || oldUserId != newUserId
            || configChanged;

        SizeChanged = oldSize != newSize;

        // Note: _hasLoadedOnce is intentionally NOT flipped here. IsFirstLoad must
        // remain true during the first OnParametersSetAsync call (per the contract
        // documented above). The flag is set at the end of OnParametersSetAsync.
        return base.SetParametersAsync(parameters);
    }

    protected override async Task OnParametersSetAsync()
    {
        // Re-check whenever the data-relevant parameters change. The same widget
        // instance can be reused across navigation (e.g. switching stores), so a
        // cached HasAccess from the previous context would otherwise leak through.
        // Always call CheckAccessAsync on data-parameter changes; it handles the
        // empty-permissions case by setting HasAccess=true and returning immediately.
        if (DataParametersChanged)
        {
            _checkCts?.Cancel();
            _checkCts?.Dispose();
            _checkCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);
            await CheckAccessAsync(_checkCts.Token);
        }

        // Mark the initial lifecycle pass complete only after the first
        // OnParametersSetAsync. Subclasses observing IsFirstLoad inside their own
        // OnParametersSetAsync now see true on the first call.
        _hasLoadedOnce = true;
    }

    private async Task CheckAccessAsync(CancellationToken ct)
    {
        // Re-evaluate from scratch each time so a previous denial doesn't carry over
        // when the widget context changes (e.g. switching stores) and now permits access.
        HasAccess = true;
        if (RequiredPermissions.Length == 0) return;

        try
        {
            ClaimsPrincipal? user = AuthState is null ? null : (await AuthState).User;
            foreach (var p in RequiredPermissions)
            {
                var ok = await AccessControl.IsAllowedAsync(
                    new WidgetDescriptor { Type = WidgetType, RequiredPermissions = new[] { p } }, user, ct);
                if (!ok)
                {
                    HasAccess = false;
                    return;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // This check was pre-empted by a newer parameter change. Do NOT touch HasAccess;
            // the superseding check owns the result.
        }
        catch
        {
            // Fail closed: any unexpected failure during the access check denies
            // access. During prerender the auth state may not yet be available; the
            // second render (after the circuit starts) re-runs CheckAccessAsync with
            // proper state and can grant access if the user really is authorized.
            HasAccess = false;
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (!HasAccess)
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "widget-access-denied");
            builder.CloseElement();
            return;
        }

        var content = ChildContent;
        if (content is not null)
        {
            builder.AddContent(2, content);
        }
    }

    protected virtual Task EnterEdit()
    {
        if (EditMode)
            return Task.CompletedTask;

        EditConfig = Config is null ? new TConfig() : Config.Deserialize<TConfig>();
        EditMode = true;
        return Task.CompletedTask;
    }

    public virtual Task CancelEdit()
    {
        EditMode = false;
        EditConfig = default;
        return Task.CompletedTask;
    }

    public virtual async Task SaveEdit()
    {
        if (EditConfig is null)
            return;

        // Persist first; only update local state and exit edit mode after the
        // callback succeeds. Otherwise a save failure leaves the UI looking saved
        // while the dashboard config never made it to storage.
        var newConfig = JsonSerializer.SerializeToNode(EditConfig)!.AsObject();
        await ConfigChanged.InvokeAsync(newConfig);
        TypedConfig = EditConfig;
        await CancelEdit();
    }

    public virtual async Task Remove()
    {
        await OnRemove.InvokeAsync();
    }

    public void Dispose()
    {
        try { _checkCts?.Cancel(); } catch (ObjectDisposedException) { }
        try { _checkCts?.Dispose(); } catch (ObjectDisposedException) { }
        _cts.Cancel();
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}
