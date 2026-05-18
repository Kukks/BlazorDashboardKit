using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BlazorDashboardKit.Services;

/// <summary>
/// Render-mode-aware wrapper around the Gridstack / chart JS interop.
/// </summary>
/// <remarks>
/// <para>
/// The original BTCPay <c>DashboardJsInterop</c> gated every JS call behind a
/// server-only <c>IsPreRendering()</c> reflection check. That does not work for a
/// library that also targets WebAssembly and static SSR, so instead the host
/// supplies an explicit <paramref name="interactive"/> flag (from
/// <c>RendererInfo.IsInteractive</c>) and JS is only invoked when interactive.
/// </para>
/// <para>
/// The global <c>window.DashboardInterop</c> object is replaced by an ESM module
/// imported from the RCL static assets path
/// <c>./_content/BlazorDashboardKit/dashboard-interop.js</c>. Only
/// <see cref="InitGridAsync"/> imports the module; every other JS-invoking method
/// is a no-op until an interactive grid has been initialized, so nothing touches
/// JS during prerender / static SSR.
/// </para>
/// </remarks>
public sealed class DashboardJsInterop : IAsyncDisposable
{
    private const string ModulePath = "./_content/BlazorDashboardKit/dashboard-interop.js";

    private readonly IJSRuntime _js;
    private IJSObjectReference? _module;
    private DotNetObjectReference<object>? _dotNetRef;

    public DashboardJsInterop(IJSRuntime js) => _js = js;

    // --- Gridstack integration ---

    /// <summary>
    /// Imports the interop module and initializes the Gridstack grid.
    /// No-op when <paramref name="interactive"/> is <c>false</c> (static SSR / prerender).
    /// </summary>
    /// <param name="dotNetHelper">
    /// Optional .NET callback reference (e.g. for <c>OnGridChanged</c>). When supplied it is
    /// cached and disposed by <see cref="DisposeAsync"/>; passing a new one replaces and
    /// disposes any previously cached reference.
    /// </param>
    public async Task InitGridAsync(
        string containerId,
        bool interactive,
        CancellationToken ct,
        DotNetObjectReference<object>? dotNetHelper = null,
        bool editMode = false)
    {
        if (!interactive)
            return;                                     // static SSR / prerender: no JS

        if (dotNetHelper is not null && !ReferenceEquals(dotNetHelper, _dotNetRef))
        {
            _dotNetRef?.Dispose();
            _dotNetRef = dotNetHelper;
        }

        _module ??= await _js.InvokeAsync<IJSObjectReference>(
            "import", ct, ModulePath);
        await _module.InvokeVoidAsync("initGrid", ct, containerId, _dotNetRef, editMode);
    }

    public async Task SetEditModeAsync(bool editMode, bool interactive, CancellationToken ct)
    {
        if (!interactive || _module is null)
            return;
        await _module.InvokeVoidAsync("setEditMode", ct, editMode);
    }

    public async Task DestroyGridAsync(bool interactive, CancellationToken ct)
    {
        if (!interactive || _module is null)
            return;
        await _module.InvokeVoidAsync("destroyGrid", ct);
    }

    public async Task AddGridWidgetAsync(ElementReference element, bool interactive, CancellationToken ct)
    {
        if (!interactive || _module is null)
            return;
        await _module.InvokeVoidAsync("addGridWidget", ct, element);
    }

    public async Task RemoveGridWidgetAsync(ElementReference element, bool interactive, CancellationToken ct)
    {
        if (!interactive || _module is null)
            return;
        await _module.InvokeVoidAsync("removeGridWidget", ct, element);
    }

    // --- Charts ---

    public async Task RenderChartAsync(
        string elementId,
        string type,
        object labels,
        object series,
        bool interactive,
        CancellationToken ct,
        object? options = null)
    {
        if (!interactive || _module is null)
            return;
        await _module.InvokeVoidAsync("renderChart", ct, elementId, type, labels, series, options);
    }

    public async Task DestroyChartAsync(string elementId, bool interactive, CancellationToken ct)
    {
        if (!interactive || _module is null)
            return;
        await _module.InvokeVoidAsync("destroyChart", ct, elementId);
    }

    // --- Export / Import (position serialization) ---

    public async Task DownloadJsonAsync(string filename, string json, bool interactive, CancellationToken ct)
    {
        if (!interactive || _module is null)
            return;
        await _module.InvokeVoidAsync("downloadJson", ct, filename, json);
    }

    public async Task<string?> ReadFileAsTextAsync(ElementReference input, bool interactive, CancellationToken ct)
    {
        if (!interactive || _module is null)
            return null;
        return await _module.InvokeAsync<string?>("readFileAsText", ct, input);
    }

    public async Task CopyToClipboardAsync(string text, bool interactive, CancellationToken ct)
    {
        if (!interactive || _module is null)
            return;
        await _module.InvokeVoidAsync("copyToClipboard", ct, text);
    }

    // --- .NET callback reference ---

    /// <summary>
    /// Caches the .NET callback reference so it can be disposed with this interop.
    /// Does not invoke JS; <see cref="InitGridAsync"/> forwards the cached reference
    /// to the grid. Passing a new reference disposes the previously cached one.
    /// </summary>
    public void RegisterDotNetRef(DotNetObjectReference<object> dotNetHelper)
    {
        if (ReferenceEquals(dotNetHelper, _dotNetRef))
            return;
        _dotNetRef?.Dispose();
        _dotNetRef = dotNetHelper;
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
        {
            try
            {
                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // Circuit already gone (e.g. browser closed): nothing to clean up JS-side.
            }
            _module = null;
        }

        _dotNetRef?.Dispose();
        _dotNetRef = null;
    }
}
