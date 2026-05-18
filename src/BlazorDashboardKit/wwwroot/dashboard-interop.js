// BlazorDashboardKit dashboard interop (ES module).
//
// Ported from BTCPay Server's classic `window.DashboardInterop` script. The
// behaviour (Gridstack init/serialize/destroy, Chartist rendering, JSON
// download/upload, clipboard) is preserved 1:1; only the delivery mechanism
// changed: the global object's methods are now named ESM exports and its
// `this._*` instance fields are module-scoped state.
//
// `GridStack` (gridstack-all.js) and `Chartist` are classic, non-module global
// libraries. This module references them defensively via `globalThis`; the
// consumer is responsible for including
// `<script src="_content/BlazorDashboardKit/gridstack/gridstack-all.js">`
// (and a Chartist build, if charts are used) before the dashboard becomes
// interactive.

let _grid = null;
let _charts = {};
let _dotNetHelper = null;
let _changeBatch = null;

// --- Gridstack integration ---

// BTCPay passed a DOM ElementReference; the host-agnostic kit passes the
// container's element id (DashboardHost._gridContainerId), so the element is
// resolved here. Everything else mirrors the original initGrid.
export function initGrid(containerId, dotNetHelper, editMode, options) {
    var containerElement = typeof containerId === 'string'
        ? document.getElementById(containerId)
        : containerId;
    if (!containerElement || typeof globalThis.GridStack === 'undefined') return;
    destroyGrid();
    _dotNetHelper = dotNetHelper;

    // options come from DashboardHost.GridOptions (camelCase via Blazor JSON).
    // Fall back to the kit defaults so older callers / null still work.
    var o = options || {};
    var columns = o.columns || 12;
    var grid = globalThis.GridStack.init({
        column: columns,
        cellHeight: o.cellHeight || 146,
        margin: (o.margin == null ? 8 : o.margin),
        float: (o.float == null ? true : o.float),
        animate: true,
        draggable: { handle: '.widget-drag-handle' },
        resizable: { handles: 'e,se,s,sw,w' },
        staticGrid: !editMode,
        columnOpts: {
            breakpoints: [{ w: (o.mobileBreakpointWidth || 992), c: (o.mobileColumns || 1) }],
            breakpointForWindow: true,
            columnMax: columns
        }
    }, containerElement);

    // Batch change events with a short debounce to avoid multiple rapid calls
    grid.on('change', function (event, items) {
        if (!items || !_dotNetHelper) return;
        // Debounce: collect changes, send once after 100ms of no activity
        if (_changeBatch) clearTimeout(_changeBatch);
        _changeBatch = setTimeout(function () {
            _changeBatch = null;
            var changes = [];
            var allNodes = grid.getGridItems();
            allNodes.forEach(function (el) {
                var node = el.gridstackNode;
                if (!node) return;
                changes.push({
                    id: node.id || '',
                    x: node.x || 0,
                    y: node.y || 0,
                    w: node.w || 1,
                    h: node.h || 1
                });
            });
            _dotNetHelper.invokeMethodAsync('OnGridChanged', JSON.stringify(changes));
        }, 100);
    });

    // Destroy charts on resize start to prevent SVG artifacts
    grid.on('resizestart', function (event, el) {
        var chartElements = el.querySelectorAll('.ct-chart');
        chartElements.forEach(function (chartEl) {
            if (chartEl.id && _charts[chartEl.id]) {
                _charts[chartEl.id].detach();
                delete _charts[chartEl.id];
                chartEl.innerHTML = '';
            }
        });
    });

    _grid = grid;
}

export function setEditMode(editMode) {
    if (!_grid) return;
    _grid.setStatic(!editMode);
}

export function destroyGrid() {
    if (_changeBatch) {
        clearTimeout(_changeBatch);
        _changeBatch = null;
    }
    if (_grid) {
        _grid.destroy(false); // false = don't remove DOM elements (Blazor owns them)
        _grid = null;
    }
    _dotNetHelper = null;
}

export function addGridWidget(element) {
    if (!_grid || !element) return;
    _grid.makeWidget(element);
}

export function removeGridWidget(element) {
    if (!_grid || !element) return;
    _grid.removeWidget(element, false); // false = don't remove DOM
}

// --- Charts ---

export function renderChart(elementId, type, labels, series, options) {
    var element = document.getElementById(elementId);
    if (!element || typeof globalThis.Chartist === 'undefined') return;
    var Chartist = globalThis.Chartist;

    if (_charts[elementId]) {
        _charts[elementId].detach();
    }

    var chartData = { labels: labels, series: series };

    var low = 0;
    if (type === 'line' && series.length > 0) {
        var flatValues = series[0];
        if (flatValues && flatValues.length > 0) {
            var min = Math.min.apply(null, flatValues);
            var max = Math.max.apply(null, flatValues);
            low = Math.max(min - ((max - min) / 5), 0);
        }
    }

    var labelCount = 6;
    var pointCount = labels.length;
    var labelEvery = pointCount / labelCount;
    var dateFormatter = new Intl.DateTimeFormat('default', { month: 'short', day: 'numeric' });

    var tooltip = (typeof Chartist.plugins !== 'undefined' && Chartist.plugins.tooltip2)
        ? Chartist.plugins.tooltip2({
            template: '<div class="chartist-tooltip-value">{{value}}</div><div class="chartist-tooltip-line"></div>',
            offset: { x: 0, y: -16 }
        })
        : null;

    var defaultOptions = {
        low: low,
        showArea: type === 'line',
        fullWidth: true,
        axisX: {
            labelInterpolationFnc: function (date, i) {
                return i % Math.ceil(labelEvery) === 0 ? dateFormatter.format(new Date(date)) : null;
            }
        },
        axisY: {
            showLabel: false,
            offset: 0
        },
        plugins: tooltip ? [tooltip] : []
    };

    var mergedOptions = Object.assign({}, defaultOptions, options || {});

    if (type === 'line') {
        _charts[elementId] = new Chartist.Line('#' + elementId, chartData, mergedOptions);
    } else if (type === 'bar') {
        _charts[elementId] = new Chartist.Bar('#' + elementId, chartData, mergedOptions);
    }
}

export function destroyChart(elementId) {
    if (_charts[elementId]) {
        _charts[elementId].detach();
        delete _charts[elementId];
    }
}

// --- Export / Import ---

export function downloadJson(filename, json) {
    var blob = new Blob([json], { type: 'application/json' });
    var url = URL.createObjectURL(blob);
    var a = document.createElement('a');
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
}

export function readFileAsText(inputElement) {
    return new Promise(function (resolve) {
        if (!inputElement || !inputElement.files || inputElement.files.length === 0) {
            resolve(null);
            return;
        }
        var reader = new FileReader();
        reader.onload = function () { resolve(reader.result); };
        reader.onerror = function () { resolve(null); };
        reader.readAsText(inputElement.files[0]);
    });
}

// --- Clipboard ---
//
// BTCPay invoked `navigator.clipboard.writeText` directly from .NET rather than
// through a DashboardInterop method. The kit's DashboardJsInterop routes every
// JS call through this module, so the same behaviour is exposed as an export.
export function copyToClipboard(text) {
    return navigator.clipboard.writeText(text);
}
