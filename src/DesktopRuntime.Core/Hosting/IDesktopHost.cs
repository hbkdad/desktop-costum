namespace DesktopRuntime.Core.Hosting;

/// <summary>A monitor as currently reported by the operating system.</summary>
/// <param name="DeviceInterfacePath">
/// Stable identity, matching <see cref="Workspaces.MonitorLayout.DeviceInterfacePath"/>.
/// Prototype 9 established this is the only identifier safe to persist.
/// </param>
public readonly record struct MonitorInfo(
    string DeviceInterfacePath,
    string? FriendlyName,
    Workspaces.Rect Bounds,
    uint Dpi,
    bool IsPrimary);

/// <summary>Enumerates the monitors currently attached.</summary>
public interface IMonitorProvider
{
    IReadOnlyList<MonitorInfo> GetMonitors();
}

/// <summary>
/// Reports whether the Tier 1 attachment surface from
/// <c>docs/architecture/adr/0003-desktop-hosting-strategy.md</c> is usable right now.
/// <para>
/// Deliberately a question asked at runtime rather than a compile-time assumption:
/// Phase 3 found attachment works on some Windows builds and not others, so the answer
/// must be discovered on the machine the product is running on.
/// </para>
/// </summary>
public interface IDesktopAttachmentProbe
{
    bool IsAttachmentSurfaceAvailable();
}

/// <summary>Applies wallpaper to the desktop.</summary>
public interface IWallpaperSurface
{
    /// <summary>
    /// Whether this surface can give different monitors different wallpapers.
    /// <para>
    /// False is a real possibility, not a placeholder: the supported
    /// <c>SystemParametersInfo</c> path validated in Phase 3 sets one wallpaper across
    /// the whole desktop. Callers must surface that limitation to the user rather than
    /// silently applying one monitor's choice everywhere.
    /// </para>
    /// </summary>
    bool SupportsPerMonitor { get; }

    /// <summary>
    /// Applies a still image. <paramref name="monitorDeviceInterfacePath"/> is ignored
    /// when <see cref="SupportsPerMonitor"/> is false.
    /// </summary>
    /// <returns>An error message on failure, or null on success.</returns>
    string? SetStaticWallpaper(string monitorDeviceInterfacePath, string imagePath);
}
