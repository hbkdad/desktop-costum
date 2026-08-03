using DesktopRuntime.DesktopHost;

namespace DesktopRuntime.DesktopHost.Tests;

/// <summary>
/// Integration tests for per-monitor wallpaper via the shell's <c>IDesktopWallpaper</c>.
/// <para>
/// Nothing here changes what the user sees: the only write test re-applies the wallpaper
/// each monitor <b>already has</b>, which exercises the full path while leaving the
/// desktop identical.
/// </para>
/// </summary>
public class PerMonitorWallpaperTests
{
    [Fact]
    public void ShellMonitorIds_MatchTheMonitorProvidersDevicePaths()
    {
        // The load-bearing assumption of per-monitor wallpaper. If these two disagree,
        // SetWallpaper would target an id the rest of the system never uses and would
        // silently affect nothing.
        using var surface = new WindowsWallpaperSurface();

        if (!surface.SupportsPerMonitor)
        {
            return; // Shell interface unavailable in this session; nothing to compare.
        }

        var shellIds = surface.GetShellMonitorIds();
        var providerIds = new WindowsMonitorProvider()
            .GetMonitors()
            .Select(m => m.DeviceInterfacePath)
            .ToList();

        if (shellIds.Count == 0 || providerIds.Count == 0)
        {
            return; // Headless agent.
        }

        // The shell may list monitors that are configured but not currently attached, so
        // the requirement is that every monitor we can see is addressable, not that the
        // two lists are identical.
        foreach (string providerId in providerIds)
        {
            Assert.Contains(shellIds, id => string.Equals(id, providerId, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void SupportsPerMonitor_IsConsistentWithBeingAbleToListMonitorIds()
    {
        using var surface = new WindowsWallpaperSurface();

        if (surface.SupportsPerMonitor)
        {
            // Claiming the capability while being unable to enumerate targets would make
            // WorkspaceActivator skip a warning the user needs.
            Assert.NotNull(surface.GetShellMonitorIds());
        }
        else
        {
            Assert.Empty(surface.GetShellMonitorIds());
        }
    }

    [Fact]
    public void PerMonitorWallpaper_CanBeReadAndReapplied()
    {
        using var surface = new WindowsWallpaperSurface();

        if (!surface.SupportsPerMonitor)
        {
            return;
        }

        var monitors = new WindowsMonitorProvider().GetMonitors();
        if (monitors.Count == 0)
        {
            return;
        }

        foreach (var monitor in monitors)
        {
            string? current = surface.GetCurrentWallpaper(monitor.DeviceInterfacePath);
            if (string.IsNullOrWhiteSpace(current) || !File.Exists(current))
            {
                continue; // No per-monitor wallpaper set — nothing to re-apply.
            }

            if (WindowsWallpaperSurface.IsTranscodedCache(current))
            {
                // Re-applying Windows' own cache would install an internal file as the
                // user's chosen wallpaper, discarding the original path.
                continue;
            }

            // Full production path, zero visible change: it is already this image.
            string? error = surface.SetStaticWallpaper(monitor.DeviceInterfacePath, current);

            Assert.Null(error);
            Assert.Equal(current, surface.GetCurrentWallpaper(monitor.DeviceInterfacePath));
        }
    }

    [Fact]
    public void GetCurrentWallpaper_ForAnUnknownMonitor_DoesNotThrow_ButAlsoDoesNotIndicateFailure()
    {
        // Observed behaviour, contrary to the obvious assumption: the shell does NOT
        // reject an unknown monitor id — it returns the desktop-wide wallpaper. So this
        // call can never be used to check whether a monitor id is valid; use
        // GetShellMonitorIds for that. Pinned here so the assumption is not made again.
        using var surface = new WindowsWallpaperSurface();
        const string unknownMonitor =
            @"\\?\DISPLAY#NOPE0000#0&0&0&UID0#{00000000-0000-0000-0000-000000000000}";

        var exception = Record.Exception(() => surface.GetCurrentWallpaper(unknownMonitor));

        Assert.Null(exception);
        Assert.DoesNotContain(unknownMonitor, surface.GetShellMonitorIds());
    }

    [Fact]
    public void DisposedSurface_RejectsFurtherUse()
    {
        var surface = new WindowsWallpaperSurface();
        surface.Dispose();

        Assert.Throws<ObjectDisposedException>(() => surface.GetCurrentWallpaper());
        Assert.Throws<ObjectDisposedException>(() => surface.SetStaticWallpaper("", "x.jpg"));
    }

    [Fact]
    public void Dispose_IsIdempotent()
    {
        var surface = new WindowsWallpaperSurface();

        surface.Dispose();
        var exception = Record.Exception(surface.Dispose);

        Assert.Null(exception);
    }

    [Fact]
    public void DisposingOneSurface_DoesNotBreakAnother()
    {
        // Regression test. An earlier version released the COM object with
        // FinalReleaseComObject; because the shell returns the same underlying object to
        // every caller and .NET shares one RCW per COM identity, that severed it for all
        // live instances — every later construction threw InvalidComObjectException.
        var first = new WindowsWallpaperSurface();
        using var second = new WindowsWallpaperSurface();

        first.Dispose();

        var exception = Record.Exception(() =>
        {
            _ = second.SupportsPerMonitor;
            _ = second.GetShellMonitorIds();
            _ = second.GetCurrentWallpaper();

            // And a surface constructed after the disposal must work too.
            using var third = new WindowsWallpaperSurface();
            _ = third.GetShellMonitorIds();
        });

        Assert.Null(exception);
    }
}
