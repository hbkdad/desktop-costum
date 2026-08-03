using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using DesktopRuntime.Core.Hosting;

namespace DesktopRuntime.DesktopHost;

/// <summary>
/// Applies still wallpaper, preferring the shell's <c>IDesktopWallpaper</c> interface —
/// which can address individual monitors — and falling back to
/// <c>SystemParametersInfo</c>, which sets one image across the whole desktop.
/// <para>
/// <see cref="SupportsPerMonitor"/> reports which of the two is actually in use, so
/// <c>WorkspaceActivator</c> can warn the user when a workspace asks for something this
/// machine cannot deliver rather than silently applying one monitor's choice everywhere.
/// </para>
/// <para>
/// Per-monitor support matters more than it might appear: mixed multi-monitor setups are
/// the defining pain of MVP-primary persona #1 in <c>docs/product/personas.md</c>.
/// </para>
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsWallpaperSurface : IWallpaperSurface, IDisposable
{
    private IDesktopWallpaper? _shellWallpaper;
    private bool _disposed;

    public WindowsWallpaperSurface() => _shellWallpaper = DesktopWallpaperFactory.TryCreate();

    public bool SupportsPerMonitor => _shellWallpaper is not null;

    /// <param name="monitorDeviceInterfacePath">
    /// The monitor to target. Ignored when <see cref="SupportsPerMonitor"/> is false, and
    /// may be empty to mean "the whole desktop" even when it is true.
    /// </param>
    public string? SetStaticWallpaper(string monitorDeviceInterfacePath, string imagePath)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (string.IsNullOrWhiteSpace(imagePath))
        {
            return "no wallpaper image was supplied.";
        }

        // An absolute path matters: these APIs resolve a relative path against the
        // calling process's current directory, which is not something a saved workspace
        // should depend on.
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
            // its own and both APIs silently produce a blank desktop for a missing file.
            return $"the wallpaper file '{fullPath}' does not exist.";
        }

        if (_shellWallpaper is not null)
        {
            string? shellError = TrySetViaShell(monitorDeviceInterfacePath, fullPath);
            if (shellError is null)
            {
                return null;
            }

            // The shell interface exists but rejected this call. Fall through to the
            // simpler API rather than failing outright — a whole-desktop wallpaper is a
            // better outcome for the user than none, and the caller is not told it
            // succeeded per-monitor when it did not.
            string? fallbackError = SetViaSystemParametersInfo(fullPath);
            return fallbackError is null ? null : $"{shellError} Fallback also failed: {fallbackError}";
        }

        return SetViaSystemParametersInfo(fullPath);
    }

    private string? TrySetViaShell(string monitorDeviceInterfacePath, string fullPath)
    {
        try
        {
            // A null monitor id means every monitor, which is the right reading of an
            // unspecified target.
            string? monitorId = string.IsNullOrWhiteSpace(monitorDeviceInterfacePath)
                ? null
                : monitorDeviceInterfacePath;

            _shellWallpaper!.SetWallpaper(monitorId, fullPath);
            return null;
        }
        catch (COMException ex)
        {
            return $"the shell wallpaper interface rejected the request (0x{ex.HResult:X8}).";
        }
    }

    private static string? SetViaSystemParametersInfo(string fullPath)
    {
        bool applied = NativeMethods.SystemParametersInfoSet(
            NativeMethods.SPI_SETDESKWALLPAPER, 0, fullPath,
            NativeMethods.SPIF_UPDATEINIFILE | NativeMethods.SPIF_SENDCHANGE);

        return applied
            ? null
            : $"Windows rejected the wallpaper change (win32 error {Marshal.GetLastWin32Error()}).";
    }

    /// <summary>
    /// The wallpaper currently set for the whole desktop, or null if it cannot be read.
    /// <para>
    /// <b>Caution:</b> Windows often returns its own transcoded cache
    /// (<c>…\Themes\TranscodedWallpaper</c>) rather than the file the user originally
    /// chose. That cache is the same image, but it is not a durable identifier — do not
    /// persist it into a workspace, and treat it as unsuitable for round-tripping an
    /// original path. Use <see cref="IsTranscodedCache"/> to detect it.
    /// </para>
    /// </summary>
    public string? GetCurrentWallpaper()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var buffer = new StringBuilder(260);

        return NativeMethods.SystemParametersInfoGet(
            NativeMethods.SPI_GETDESKWALLPAPER, (uint)buffer.Capacity, buffer, 0)
            ? buffer.ToString()
            : null;
    }

    /// <summary>
    /// The wallpaper currently set for one monitor, or null if per-monitor wallpaper is
    /// unavailable or cannot be read.
    /// </summary>
    public string? GetCurrentWallpaper(string monitorDeviceInterfacePath)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_shellWallpaper is null || string.IsNullOrWhiteSpace(monitorDeviceInterfacePath))
        {
            return null;
        }

        try
        {
            string path = _shellWallpaper.GetWallpaper(monitorDeviceInterfacePath);
            return string.IsNullOrEmpty(path) ? null : path;
        }
        catch (COMException)
        {
            return null;
        }
    }

    /// <summary>
    /// Whether a path is Windows' internal transcoded wallpaper cache rather than a file
    /// the user chose. Such a path should never be stored in a workspace: it is shared,
    /// overwritten whenever the wallpaper changes, and meaningless on another machine.
    /// </summary>
    public static bool IsTranscodedCache(string? path) =>
        !string.IsNullOrWhiteSpace(path) &&
        Path.GetFileName(path).StartsWith("TranscodedWallpaper", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Monitor identifiers as the shell reports them. Exposed mainly so tests can confirm
    /// these agree with <see cref="WindowsMonitorProvider"/>'s device interface paths —
    /// the two must match, or per-monitor wallpaper would silently target nothing.
    /// </summary>
    public IReadOnlyList<string> GetShellMonitorIds()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_shellWallpaper is null)
        {
            return [];
        }

        try
        {
            uint count = _shellWallpaper.GetMonitorDevicePathCount();
            var ids = new List<string>((int)count);

            for (uint i = 0; i < count; i++)
            {
                string id = _shellWallpaper.GetMonitorDevicePathAt(i);
                if (!string.IsNullOrEmpty(id))
                {
                    ids.Add(id);
                }
            }

            return ids;
        }
        catch (COMException)
        {
            return [];
        }
    }

    /// <summary>
    /// Releases this instance's reference to the shell object.
    /// <para>
    /// Deliberately does <b>not</b> call <c>Marshal.FinalReleaseComObject</c>. The shell
    /// hands back the same underlying object to every caller, and .NET shares one RCW per
    /// COM identity — so forcibly destroying it here severed it for every other live
    /// instance, which surfaced as
    /// <c>InvalidComObjectException: COM object that has been separated from its
    /// underlying RCW</c> in an unrelated test. Dropping the reference and letting the
    /// runtime release it when nothing holds it is both correct and the documented
    /// guidance.
    /// </para>
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        _shellWallpaper = null;
    }
}
