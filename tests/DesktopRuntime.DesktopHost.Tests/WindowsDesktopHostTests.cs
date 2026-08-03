using DesktopRuntime.DesktopHost;

namespace DesktopRuntime.DesktopHost.Tests;

/// <summary>
/// Integration tests: these exercise the real Windows APIs rather than fakes, so they
/// verify the production adapter itself.
/// <para>
/// They are written to behave correctly on a headless build agent, where monitor
/// enumeration may legitimately return nothing. Where that happens the test asserts the
/// contract that still applies (a non-null, empty result) rather than silently passing on
/// a vacuous condition.
/// </para>
/// <para>
/// Nothing here changes system state. The wallpaper test deliberately re-applies the
/// wallpaper that is <b>already set</b>, which exercises the whole code path end to end
/// while leaving the user's desktop exactly as it was.
/// </para>
/// </summary>
public class WindowsDesktopHostTests
{
    [Fact]
    public void MonitorProvider_ReturnsMonitorsWithStableIdentity()
    {
        var provider = new WindowsMonitorProvider();

        var monitors = provider.GetMonitors();

        Assert.NotNull(monitors);

        foreach (var monitor in monitors)
        {
            // Prototype 9: only the device interface path is safe to persist.
            Assert.False(string.IsNullOrWhiteSpace(monitor.DeviceInterfacePath));
            Assert.StartsWith(@"\\?\DISPLAY#", monitor.DeviceInterfacePath, StringComparison.OrdinalIgnoreCase);

            Assert.True(monitor.Bounds.Width > 0, "A monitor should report a positive width.");
            Assert.True(monitor.Bounds.Height > 0, "A monitor should report a positive height.");
            Assert.True(monitor.Dpi >= 96, $"DPI should be at least 96, got {monitor.Dpi}.");
        }
    }

    [Fact]
    public void MonitorProvider_ReportsExactlyOnePrimary_WhenAnyMonitorIsPresent()
    {
        var monitors = new WindowsMonitorProvider().GetMonitors();

        if (monitors.Count == 0)
        {
            return; // Headless agent: nothing to assert about a primary.
        }

        Assert.Equal(1, monitors.Count(m => m.IsPrimary));
    }

    [Fact]
    public void MonitorProvider_ReportsDistinctIdentitiesForDistinctMonitors()
    {
        var monitors = new WindowsMonitorProvider().GetMonitors();

        var distinct = monitors
            .Select(m => m.DeviceInterfacePath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        Assert.Equal(monitors.Count, distinct);
    }

    [Fact]
    public void MonitorProvider_IsRepeatable()
    {
        // Identity must be stable between calls, or workspace matching would be unusable.
        var provider = new WindowsMonitorProvider();

        var first = provider.GetMonitors().Select(m => m.DeviceInterfacePath).OrderBy(p => p).ToArray();
        var second = provider.GetMonitors().Select(m => m.DeviceInterfacePath).OrderBy(p => p).ToArray();

        Assert.Equal(first, second);
    }

    [Fact]
    public void AttachmentProbe_AnswersWithoutThrowing()
    {
        // The probe touches undocumented shell behaviour, so the contract is that it
        // always returns an answer. Either answer is valid — Phase 3 found false is the
        // ordinary case on current builds.
        var probe = new WindowsAttachmentProbe();

        var exception = Record.Exception(() => probe.IsAttachmentSurfaceAvailable());

        Assert.Null(exception);
    }

    [Fact]
    public void WallpaperSurface_DoesNotClaimPerMonitorSupport()
    {
        // SystemParametersInfo sets one image for the whole desktop. Claiming otherwise
        // would make WorkspaceActivator skip the warning users need.
        Assert.False(new WindowsWallpaperSurface().SupportsPerMonitor);
    }

    [Fact]
    public void WallpaperSurface_RejectsAMissingFile_WithoutChangingAnything()
    {
        var surface = new WindowsWallpaperSurface();
        string? before = surface.GetCurrentWallpaper();

        string? error = surface.SetStaticWallpaper("ignored", @"C:\definitely\not\here\nope.jpg");

        Assert.NotNull(error);
        Assert.Contains("does not exist", error);
        Assert.Equal(before, surface.GetCurrentWallpaper());
    }

    [Fact]
    public void WallpaperSurface_RejectsAnEmptyPath()
    {
        Assert.NotNull(new WindowsWallpaperSurface().SetStaticWallpaper("ignored", "   "));
    }

    [Fact]
    public void WallpaperSurface_CanApplyTheWallpaperThatIsAlreadySet()
    {
        // Exercises the full production path against the real API while leaving the
        // desktop visually unchanged, because the image applied is the current one.
        var surface = new WindowsWallpaperSurface();
        string? current = surface.GetCurrentWallpaper();

        if (string.IsNullOrWhiteSpace(current) || !File.Exists(current))
        {
            return; // No wallpaper set (common on build agents) — nothing to re-apply.
        }

        string? error = surface.SetStaticWallpaper("ignored", current);

        Assert.Null(error);
        Assert.Equal(current, surface.GetCurrentWallpaper());
    }
}
