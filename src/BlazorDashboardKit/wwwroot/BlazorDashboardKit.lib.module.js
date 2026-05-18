// Blazor JS initializer (auto-discovered for this RCL by its file name).
//
// It injects the kit's static assets into the host document so a consumer no
// longer has to add <link>/<script> tags by hand:
//   - GridStack CSS + the kit's themeable dashboard.css
//   - the GridStack global script (the ESM interop references globalThis.GridStack)
//
// Every injection is idempotent: if the host already references an asset (older
// setup, or deliberate control over ordering) nothing is duplicated. The kit's
// own ESM interop (dashboard-interop.js) is imported by the library itself and
// is intentionally NOT injected here.

const BASE = '_content/BlazorDashboardKit/';

function ensureStylesheet(href) {
    const exists = [...document.querySelectorAll('link[rel="stylesheet"]')]
        .some(l => l.getAttribute('href') === href);
    if (exists) return;
    const link = document.createElement('link');
    link.rel = 'stylesheet';
    link.href = href;
    document.head.appendChild(link);
}

function ensureScript(src) {
    const exists = [...document.querySelectorAll('script')]
        .some(s => s.getAttribute('src') === src);
    if (exists) return;
    const script = document.createElement('script');
    script.src = src;
    // Not async: GridStack must define globalThis.GridStack before the kit's
    // interop module (lazily imported once a dashboard turns interactive) runs.
    script.async = false;
    document.head.appendChild(script);
}

function injectAssets() {
    ensureStylesheet(BASE + 'gridstack/gridstack.min.css');
    ensureStylesheet(BASE + 'dashboard.css');
    ensureScript(BASE + 'gridstack/gridstack-all.js');
}

// Blazor Web App (interactive Server/WebAssembly via blazor.web.js).
export function beforeWebStart() { injectAssets(); }

// Standalone Blazor Server / WebAssembly (blazor.server.js / blazor.webassembly.js).
export function beforeStart() { injectAssets(); }
