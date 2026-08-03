using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using DesktopRuntime.Core.Hosting;
using DesktopRuntime.Core.Workspaces;

namespace DesktopRuntime.DesktopHost;

/// <summary>
/// Enumerates monitors using supported Windows APIs, reporting the device interface path
/// as identity.
/// <para>
/// Prototype 9 established two things this implementation depends on: only the device
/// interface path carries hardware identity (GDI device names and <c>HMONITOR</c> handles
/// are positional or runtime-scoped), and per-monitor DPI awareness must be set
/// <b>before</b> any geometry is read, or Windows reports virtualized coordinates and the
/// wrong values get persisted.
/// </para>
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsMonitorProvider : IMonitorProvider
{
    private static bool _dpiAwarenessAttempted;

    public WindowsMonitorProvider() => EnsureDpiAwareness();

    /// <summary>
    /// Set once per process, as early as possible. Failure is not fatal — it may already
    /// have been set by the host application or an app manifest, which is the normal case
    /// in a real application.
    /// </summary>
    private static void EnsureDpiAwareness()
    {
        if (_dpiAwarenessAttempted) return;

        _dpiAwarenessAttempted = true;
        NativeMethods.SetProcessDpiAwarenessContext(NativeMethods.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
    }

    public IReadOnlyList<MonitorInfo> GetMonitors()
    {
        var handles = new List<IntPtr>();
        NativeMethods.EnumDisplayMonitors(
            IntPtr.Zero, IntPtr.Zero, (handle, _, _, _) => { handles.Add(handle); return true; }, IntPtr.Zero);

        var monitors = new List<MonitorInfo>(handles.Count);

        foreach (IntPtr handle in handles)
        {
            var raw = new NativeMethods.MONITORINFOEX
            {
                cbSize = (uint)Marshal.SizeOf<NativeMethods.MONITORINFOEX>()
            };

            if (!NativeMethods.GetMonitorInfoW(handle, ref raw))
            {
                continue;
            }

            string? devicePath = TryGetDeviceInterfacePath(raw.szDevice, out string? friendlyName);
            if (devicePath is null)
            {
                // Without a stable identity there is nothing safe to persist against, so
                // the monitor is skipped rather than recorded under an unstable key.
                continue;
            }

            uint dpi = 96;
            if (NativeMethods.GetDpiForMonitor(handle, NativeMethods.MDT_EFFECTIVE_DPI, out uint dpiX, out _) == 0)
            {
                dpi = dpiX;
            }

            monitors.Add(new MonitorInfo(
                devicePath,
                friendlyName,
                new Rect(
                    raw.rcMonitor.Left,
                    raw.rcMonitor.Top,
                    raw.rcMonitor.Right - raw.rcMonitor.Left,
                    raw.rcMonitor.Bottom - raw.rcMonitor.Top),
                dpi,
                (raw.dwFlags & NativeMethods.MONITORINFOF_PRIMARY) != 0));
        }

        return monitors;
    }

    private static string? TryGetDeviceInterfacePath(string gdiDeviceName, out string? friendlyName)
    {
        friendlyName = null;

        var device = new NativeMethods.DISPLAY_DEVICE
        {
            cb = (uint)Marshal.SizeOf<NativeMethods.DISPLAY_DEVICE>()
        };

        if (!NativeMethods.EnumDisplayDevicesW(
                gdiDeviceName, 0, ref device, NativeMethods.EDD_GET_DEVICE_INTERFACE_NAME))
        {
            return null;
        }

        friendlyName = string.IsNullOrWhiteSpace(device.DeviceString) ? null : device.DeviceString;
        return string.IsNullOrWhiteSpace(device.DeviceID) ? null : device.DeviceID;
    }
}
