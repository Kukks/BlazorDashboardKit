using BlazorDashboardKit.Services;
using Bunit;
using Microsoft.JSInterop;
using Xunit;

namespace BlazorDashboardKit.Tests.Services;

public class DashboardJsInteropTests : TestContext
{
    [Fact]
    public async Task InitGrid_NoOps_When_Not_Interactive()
    {
        JSInterop.Mode = JSRuntimeMode.Strict;            // any unexpected JS invocation throws
        var interop = new DashboardJsInterop(JSInterop.JSRuntime);
        await interop.InitGridAsync("grid-el", interactive: false, default);   // must NOT touch JS
        await interop.DisposeAsync();
        // Strict mode + no expectation set => test fails if any JS call happened
    }

    [Fact]
    public async Task InitGrid_Imports_Module_And_Calls_initGrid_When_Interactive()
    {
        var module = JSInterop.SetupModule("./_content/BlazorDashboardKit/dashboard-interop.js");
        module.SetupVoid("initGrid", _ => true);
        var interop = new DashboardJsInterop(JSInterop.JSRuntime);
        await interop.InitGridAsync("grid-el", interactive: true, default);
        module.VerifyInvoke("initGrid");
        await interop.DisposeAsync();
    }
}
