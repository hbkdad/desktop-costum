using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using DesktopRuntime.Core.Hosting;

namespace DesktopRuntime.DesktopHost;

/// <summary>
/// Applies still wallpaper through <c>SystemParametersInfo</c> — the Tier 2 path from
/// ADR-0003, and the only one Phase 3 found to be reliably available.
/// <para>
/// <see cref="SupportsPerMonitor"/> is <c>false</c> and that is the truth, not a stub:
/// this API sets one image across the whole desktop. True per-monitor assignment needs
/// the <c>IDesktopWallpaper</c> COM interface, which has not been validated yet. Reporting
/// the limitation honestly lets <c>WorkspaceActivator</c> warn the user instead of
/// silently applying one monitor's choice everywhere.
/// </para>
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsWallpaperSurface : IWallpaperSurface
{
    public bool SupportsPerMonitor => false;

    /// <param name="monitorDeviceInterfacePath">
    /// Ignored: this surface cannot target an individual monitor. See <see cref="SupportsPerMonitor"/>.
    /// </param>
    public string? SetStaticWallpaper(string monitorDeviceInterfacePath, string imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            return "no wallpaper image was supplied.";
        }

        // An absolute path matters: the API resolves a relative path against the calling
        // process's current directory, which is not something a saved workspace should
        // depend on.
        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(imagePath);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return $"the wallpaper path '{imagePath}' is not valid: {ex.Message}";
        }

        if (!File.Exists(fullPath))
        {
            // Checked here as well as in the activator, because this class is usable on
            // its own and the API silently produces a blank desktop for a missing file.
            return $"the wallpaper file '{fullPath}' does not exist.";
        }

        bool applied = NativeMethods.SystemParametersInfoSet(
            NativeMethods.SPI_SETDESKWALLPAPER, 0, fullPath,
            NativeMethods.SPIF_UPDATEINIFILE | NativeMethods.SPIF_SENDCHANGE);

        if (!applied)
        {
            return $"Windows rejected the wallpaper change (win32 error {Marshal.GetLastWin32Error()}).";
        }

        return null;
    }

    /// <summary>
    /// The wallpaper currently set, or null if it cannot be read. Callers that change the
    /// wallpaper should capture this first so the previous value can be restored.
    /// </summary>
    public string? GetCurrentWallpaper()
    {
        var buffer = new StringBuilder(260);

        return NativeMethods.SystemParametersInfoGet(
            NativeMethods.SPI_GETDESKWALLPAPER, (uint)buffer.Capacity, buffer, 0)
            ? buffer.ToString()
            : null;
    }
}
